shader_type spatial;
render_mode unshaded, blend_premul_alpha, cull_disabled, depth_draw_never;

uniform vec3 planet_center = vec3(0.0, 0.0, 0.0);
uniform float planet_radius = 220.0;
uniform float atmosphere_radius = 250.0;
uniform vec3 inv_wavelength4 = vec3(0.6, 0.85, 1.0);
uniform vec3 sun_direction = vec3(0.0, -1.0, 0.0);
uniform float rayleigh_strength = 1.0;
uniform float density_falloff = 4.0;
uniform float atmosphere_alpha = 1.0;
uniform float pressure_scale_height = 0.28;
uniform float twilight_red_strength = 1.2;
uniform float horizon_power = 2.0;

varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec3 to_center = world_pos - planet_center;
    vec3 normal = normalize(to_center);

    vec3 view_dir = normalize(CAMERA_POSITION_WORLD - world_pos);
    vec3 light_dir = normalize(-sun_direction);

    // Horizon path length term. Stronger near tangent views, smooth across all altitudes.
    float ndotv = abs(dot(normal, view_dir));
    float horizon = pow(clamp(1.0 - ndotv, 0.0, 1.0), horizon_power);

    float thickness = max(0.001, atmosphere_radius - planet_radius);
    float view_height = length(CAMERA_POSITION_WORLD - planet_center);
    float h01 = max(0.0, (view_height - planet_radius) / thickness);

    // Continuous pressure drop with altitude: p(h) ~ exp(-h/H)
    float scale_h = max(0.02, pressure_scale_height);
    float pressure = exp(-h01 / scale_h);
    float altitude_density = exp(-h01 * density_falloff * 0.12);

    float mu = clamp(dot(view_dir, light_dir), -1.0, 1.0);
    float phase = 0.0596831 * (1.0 + mu * mu);
    float sun_mu = clamp(dot(normal, light_dir), -1.0, 1.0);
    float day = clamp(sun_mu * 0.5 + 0.5, 0.0, 1.0);

    // Longer path through atmosphere near terminator causes red-shifted sunlight.
    float air_mass = 1.0 / max(0.08, day * 0.92 + 0.08);
    float twilight_band = 1.0 - smoothstep(-0.35, 0.25, sun_mu);
    vec3 sun_transmittance = exp(-inv_wavelength4 * twilight_red_strength * air_mass * pressure * 1.45);
    vec3 sun_color = mix(vec3(1.0), sun_transmittance, twilight_band);

    float optical_depth = (0.28 + 0.72 * horizon) * altitude_density * pressure;
    float energy = rayleigh_strength * optical_depth * (0.45 + 0.55 * phase) * (0.30 + 0.70 * day);
    float ambient_energy = rayleigh_strength * 0.03 * pressure;

    vec3 scatter = inv_wavelength4 * sun_color * energy + inv_wavelength4 * ambient_energy;
    float alpha = clamp((horizon * pressure * 0.95 + length(scatter) * 3.2) * atmosphere_alpha, 0.0, 1.0);

    ALBEDO = vec3(0.0);
    EMISSION = scatter * 3.0;
    ALPHA = alpha;
}
