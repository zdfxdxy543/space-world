shader_type spatial;
render_mode unshaded, blend_add, cull_front, depth_draw_never, depth_test_disabled;

uniform vec3  planet_center             = vec3(0.0, 0.0, 0.0);
uniform float planet_radius             = 220.0;
uniform float atmosphere_radius         = 272.0;
uniform vec3  rayleigh_scattering_coeff = vec3(0.58, 1.35, 3.31);
uniform vec3  sun_direction             = vec3(0.0, -1.0, 0.0);
uniform float sea_level_pressure        = 1.0;
uniform float visual_pressure           = 1.0;
uniform float density_scale_height      = 12.0;
uniform float density_falloff           = 4.6;
uniform float atmosphere_alpha          = 0.68;
uniform float twilight_red_strength     = 1.35;
uniform float horizon_power             = 1.9;
uniform float scatter_exposure          = 1.0;
uniform float ambient_scatter           = 0.24;

varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

// ── 密度函数：除以大气厚度归一化，防止光学深度爆炸 ──────────
// 不归一化时：view_od ≈ density_at_surface × path_len ≈ 3.0 × 37 ≈ 111
//   → rayleigh_coeff × view_od ≈ 8.28 × 111 ≈ 920 → transmittance = exp(-920) ≈ 0
// 归一化后：view_od ≈ visual_pressure × PressureScaleHeight ≈ 0.24
//   → rayleigh_coeff × view_od ≈ 8.28 × 0.24 ≈ 2.0 → transmittance = exp(-2.0) ≈ 0.13 ✓
float density_at_altitude(float altitude) {
    float h         = max(0.0, altitude);
    float thickness = max(1.0, atmosphere_radius - planet_radius);

    // 指数衰减基础密度
    float base_density = exp(-h / max(0.001, density_scale_height));
    // 顶层额外收紧衰减（对应原始 density_falloff 参数）
    float top_falloff  = exp(-h / thickness * max(0.001, density_falloff));

    // ★ 关键修复：除以 thickness，使积分光学深度与大气厚度无关
    return base_density * top_falloff * max(0.0, visual_pressure) / thickness;
}

void ray_sphere_intersect(
    vec3 origin, vec3 dir, vec3 center, float radius,
    out float t_near, out float t_far
) {
    vec3  oc   = origin - center;
    float b    = dot(oc, dir);
    float c    = dot(oc, oc) - radius * radius;
    float disc = b * b - c;
    float sq   = sqrt(max(0.0, disc));
    t_near = (disc >= 0.0) ? (-b - sq) : 1e9;
    t_far  = (disc >= 0.0) ? (-b + sq) : -1e9;
}

float sun_optical_depth(vec3 pos, vec3 light_dir) {
    float t_near, t_far;
    ray_sphere_intersect(pos, light_dir, planet_center, atmosphere_radius, t_near, t_far);
    float ray_len   = max(0.0, t_far);
    const int STEPS = 6;
    float step_size = ray_len / float(STEPS);
    float od        = 0.0;
    for (int i = 0; i < STEPS; i++) {
        vec3  p   = pos + light_dir * (float(i) + 0.5) * step_size;
        float alt = max(0.0, length(p - planet_center) - planet_radius);
        od += density_at_altitude(alt) * step_size;
    }
    return od;
}

void fragment() {
    vec3 camera_pos = CAMERA_POSITION_WORLD;
    vec3 ray_dir    = normalize(world_pos - camera_pos);
    vec3 light_dir  = normalize(-sun_direction);

    float ta_near, ta_far;
    ray_sphere_intersect(camera_pos, ray_dir, planet_center, atmosphere_radius, ta_near, ta_far);
    bool hit_atm = (ta_far > ta_near) && (ta_far > 0.0);

    if (!hit_atm) { discard; }

    float tp_near, tp_far;
    ray_sphere_intersect(camera_pos, ray_dir, planet_center, planet_radius, tp_near, tp_far);
    bool hit_planet = (tp_far > tp_near) && (tp_near > 0.001);

    float t_start    = max(0.0, ta_near);
    float t_end      = hit_planet ? min(ta_far, tp_near) : ta_far;
    float ray_length = t_end - t_start;

    if (ray_length <= 0.001) { discard; }

    float phase_cos = dot(ray_dir, light_dir);
    float phase     = 0.05968310365 * (1.0 + phase_cos * phase_cos);

    const int VIEW_STEPS = 12;
    float step_size = ray_length / float(VIEW_STEPS);

    vec3  scatter = vec3(0.0);
    float view_od = 0.0;

    for (int i = 0; i < VIEW_STEPS; i++) {
        float t          = t_start + (float(i) + 0.5) * step_size;
        vec3  sample_pos = camera_pos + ray_dir * t;
        float alt        = max(0.0, length(sample_pos - planet_center) - planet_radius);
        float density    = density_at_altitude(alt);

        view_od += density * step_size;

        // 行星阴影判断
        float ts_near, ts_far;
        ray_sphere_intersect(sample_pos, light_dir, planet_center, planet_radius, ts_near, ts_far);
        bool in_shadow = (ts_far > ts_near) && (ts_near > 0.001);
        float contrib = (density > 1e-6 && !in_shadow) ? 1.0 : 0.0;

        float s_od = sun_optical_depth(sample_pos, light_dir);

        // 黄昏/黎明红色增强
        float ndotl    = dot(normalize(sample_pos - planet_center), light_dir);
        float twilight  = 1.0 - smoothstep(-0.25, 0.15, ndotl);
        float red_boost = mix(1.0, max(1.0, twilight_red_strength), twilight);

        // 消光（视线 + 太阳双向光学深度）
        vec3 total_od = rayleigh_scattering_coeff * (view_od + s_od);
        vec3 transmittance = vec3(
            exp(-total_od.r * red_boost * 0.45),
            exp(-total_od.g * red_boost * 0.75),
            exp(-total_od.b * red_boost * 1.25)
        );

        scatter += rayleigh_scattering_coeff * density * transmittance * phase * step_size * contrib;
    }

    // 环境散射（夜侧补光）
    float ambient_weight = clamp(view_od / max(0.001, density_scale_height / max(1.0, atmosphere_radius - planet_radius)), 0.0, 1.0);
    scatter += rayleigh_scattering_coeff * (ambient_scatter * 0.45) * ambient_weight;

    scatter *= max(0.0, scatter_exposure);

    ALBEDO   = vec3(0.0);
    EMISSION = scatter;
    ALPHA    = clamp(length(scatter) * 2.5 * atmosphere_alpha, 0.0, 1.0);
}
