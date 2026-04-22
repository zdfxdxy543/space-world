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
    float shell_h = max(0.0001, atmosphere_radius - planet_radius);
    float transition_band = max(0.65, shell_h * 0.65);
    float inside_factor = smoothstep(
        atmosphere_radius + transition_band,
        atmosphere_radius - transition_band,
        cam_radius);
    bool inside_atmosphere = inside_factor > 0.5;
    vec3 cam_up = cam_radius > 0.0001 ? cam_from_center / cam_radius : vec3(0.0, 1.0, 0.0);

    // Blend side visibility near the boundary to avoid hard popping when crossing the shell.
    float face_sign = dot(up, view_dir);
    float outside_side = smoothstep(-0.12, 0.12, face_sign);
    float inside_side = smoothstep(0.12, -0.12, face_sign);
    float side_visibility = mix(outside_side, inside_side, inside_factor);
    if (side_visibility <= 0.001) {
        discard;
    }

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
    float twilight = mix(twilight_outer, twilight_inner, inside_factor);

    float mu_view = dot(-view_dir, sun_dir);

    float phase_rayleigh = 0.75 * (1.0 + mu_view * mu_view);
    float g = 0.76;
    float denom = pow(max(0.1, 1.0 + g * g - 2.0 * g * mu_view), 1.5);
    float phase_mie = (1.0 - g * g) / (4.0 * 3.14159265 * denom);

    float horizon_power = mix(1.05, 0.78, inside_factor);
    float horizon = pow(clamp(horizon_weight, 0.0, 1.0), horizon_power);

    float day_visibility = mix(0.12, 1.0, day);
    vec3 sky = rayleigh_rgb * phase_rayleigh * scatter_amount * day_visibility * (0.16 + day * 0.56 + horizon * 0.5);
    vec3 haze = mie_rgb * phase_mie * scatter_amount * day_visibility * (0.06 + day * 0.2 + horizon * 0.14);

    float toward_sun_power = mix(1.2, 0.85, inside_factor);
    float toward_sun = pow(max(dot(-view_dir, sun_dir), 0.0), toward_sun_power);
    float view_horizon = 1.0 - clamp(dot(up, cam_up), 0.0, 1.0);
    float sunset_horizon_power = mix(0.82, 0.72, inside_factor);
    float sunset_horizon_mask = pow(view_horizon, sunset_horizon_power);
    float sunset_inside_boost = mix(1.0, 1.9, inside_factor);
    vec3 sunset_tint = vec3(1.15, 0.5, 0.26) * twilight * scatter_amount * toward_sun * sunset_horizon_mask * sunset_strength * sunset_inside_boost;

    vec3 absorb = ozone_absorption_rgb * (0.2 + (1.0 - day) * 0.52) * scatter_amount;
    vec3 color = max(vec3(0.0), sky + haze + sunset_tint - absorb);

    ALBEDO = color;
    float twilight_alpha_boost = mix(0.2, 0.42, inside_factor);
    float alpha_day_term = 0.18 + day * 0.82 + twilight * twilight_alpha_boost;
    float alpha_cap = mix(0.52, 0.82, inside_factor);
    float alpha = clamp((0.03 + scatter_amount * (0.16 + horizon * 0.42)) * alpha_day_term, 0.0, alpha_cap);

    // External view: suppress hard limb ring without killing shell visibility.
    float outside_factor = 1.0 - inside_factor;
    float limb_soften = 1.0 - 0.2 * outside_factor * smoothstep(0.58, 0.98, horizon);

    ALPHA = alpha * side_visibility * limb_soften;
}
