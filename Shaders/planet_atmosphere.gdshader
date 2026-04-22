shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_never;

uniform vec3 planet_center_ws = vec3(0.0);
uniform vec3 sun_dir_ws = vec3(0.354f, 0.911f, 0.202f);

uniform float planet_radius = 220.0;
uniform float atmosphere_radius = 225.0;
uniform float atmosphere_density : hint_range(0.1, 4.0, 0.01) = 1.0;
uniform float sunset_strength : hint_range(0.0, 3.0, 0.01) = 1.0;

uniform vec3 rayleigh_rgb = vec3(0.18, 0.32, 0.58);
uniform vec3 mie_rgb = vec3(0.03, 0.03, 0.028);
uniform vec3 ozone_absorption_rgb = vec3(0.0);

varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec3 up = normalize(world_pos - planet_center_ws);
    vec3 view_dir = normalize(CAMERA_POSITION_WORLD - world_pos);
    vec3 sun_dir = normalize(sun_dir_ws);

    vec3 cam_from_center = CAMERA_POSITION_WORLD - planet_center_ws;
    float cam_radius = length(cam_from_center);
    bool inside_atmosphere = cam_radius < atmosphere_radius;
    vec3 cam_up = cam_radius > 0.0001 ? cam_from_center / cam_radius : vec3(0.0, 1.0, 0.0);

    // Render only the geometrically correct side of the shell.
    float face_sign = dot(up, view_dir);
    if (!inside_atmosphere && face_sign <= 0.0) {
        discard;
    }
    if (inside_atmosphere && face_sign >= 0.0) {
        discard;
    }

    float shell_h = max(0.0001, atmosphere_radius - planet_radius);
    float cam_h01 = clamp((cam_radius - planet_radius) / shell_h, 0.0, 1.0);
    float mu = clamp(abs(face_sign), 0.02, 1.0);

    // Air mass approximation: much stronger near horizon, modest at disc center.
    float horizon_weight = 1.0 - mu;
    float air_mass = 1.0 / (mu * 0.92 + 0.08);
    float density = atmosphere_density * mix(1.0, 0.68, clamp(cam_h01, 0.0, 1.0));
    float scatter_amount = 1.0 - exp(-air_mass * density * (0.09 + horizon_weight * 0.36));

    float mu_sun = dot(up, sun_dir);
    float day = smoothstep(-0.08, 0.2, mu_sun);
    float twilight_outer = smoothstep(-0.30, 0.05, mu_sun) * (1.0 - smoothstep(0.05, 0.26, mu_sun));
    float twilight_inner = smoothstep(-0.38, 0.08, mu_sun) * (1.0 - smoothstep(0.08, 0.34, mu_sun));
    float twilight = inside_atmosphere ? twilight_inner : twilight_outer;

    float mu_view = dot(-view_dir, sun_dir);

    float phase_rayleigh = 0.75 * (1.0 + mu_view * mu_view);
    float g = 0.76;
    float denom = pow(max(0.1, 1.0 + g * g - 2.0 * g * mu_view), 1.5);
    float phase_mie = (1.0 - g * g) / (4.0 * 3.14159265 * denom);

    float horizon = pow(clamp(horizon_weight, 0.0, 1.0), inside_atmosphere ? 0.72 : 1.2);

    float day_visibility = mix(0.12, 1.0, day);
    vec3 sky = rayleigh_rgb * phase_rayleigh * scatter_amount * day_visibility * (0.14 + day * 0.58 + horizon * 0.72);
    vec3 haze = mie_rgb * phase_mie * scatter_amount * day_visibility * (0.05 + day * 0.22 + horizon * 0.2);

    float toward_sun = pow(max(dot(-view_dir, sun_dir), 0.0), inside_atmosphere ? 0.85 : 1.2);
    float view_horizon = 1.0 - clamp(dot(up, cam_up), 0.0, 1.0);
    float sunset_horizon_mask = pow(view_horizon, inside_atmosphere ? 0.72 : 0.82);
    float sunset_inside_boost = inside_atmosphere ? 1.9 : 1.0;
    vec3 sunset_tint = vec3(1.15, 0.5, 0.26) * twilight * scatter_amount * toward_sun * sunset_horizon_mask * sunset_strength * sunset_inside_boost;

    vec3 absorb = ozone_absorption_rgb * (0.2 + (1.0 - day) * 0.52) * scatter_amount;
    vec3 color = max(vec3(0.0), sky + haze + sunset_tint - absorb);

    ALBEDO = color;
    float alpha_day_term = 0.18 + day * 0.82 + twilight * (inside_atmosphere ? 0.42 : 0.2);
    float alpha = clamp((0.02 + scatter_amount * (0.12 + horizon * 0.68)) * alpha_day_term, 0.0, inside_atmosphere ? 0.82 : 0.52);
    ALPHA = alpha;
}
