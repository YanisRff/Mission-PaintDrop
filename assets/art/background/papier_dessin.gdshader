shader_type canvas_item;
// Fond "papier à dessin" : surface lisse, grain fin et léger relief.
// Godot 4.x - à appliquer sur un ColorRect / Sprite2D / TextureRect.

// ---------- Couleurs ----------
uniform vec4 paper_color : source_color = vec4(0.96, 0.94, 0.88, 1.0);
uniform vec4 shadow_color : source_color = vec4(0.78, 0.73, 0.64, 1.0);

// ---------- Relief ----------
// Taille (en pixels) des ondulations douces du papier
uniform float relief_size : hint_range(20.0, 600.0) = 160.0;
uniform float relief_amount : hint_range(0.0, 2.0) = 0.6;

// ---------- Grain (la "dent" du papier) ----------
uniform float grain_size : hint_range(0.5, 8.0) = 2.0;
uniform float grain_amount : hint_range(0.0, 1.0) = 0.25;

// ---------- Fibres ----------
uniform float fiber_length : hint_range(2.0, 60.0) = 14.0;
uniform float fiber_amount : hint_range(0.0, 1.0) = 0.12;

// ---------- Lumière ----------
uniform vec2 light_direction = vec2(-0.6, -0.8);
uniform float light_height : hint_range(0.2, 3.0) = 1.2;
uniform float bump_strength : hint_range(0.0, 10.0) = 3.0;
uniform float light_strength : hint_range(0.0, 2.0) = 0.5;

// ---------- Vignettage ----------
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.15;

varying vec2 world_pos;

void vertex() {
	// Le papier reste "collé" au monde : il défile avec la caméra
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

// ---------- Bruit ----------
float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float vnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
	float v = 0.0;
	float a = 0.5;
	for (int i = 0; i < 4; i++) {
		v += a * vnoise(p);
		p = p * 2.03 + vec2(17.1, 9.2);
		a *= 0.5;
	}
	return v;
}

// Fibres : bruit étiré dans une direction, à deux orientations
float fibers(vec2 p) {
	mat2 r1 = mat2(vec2(0.8, -0.6), vec2(0.6, 0.8));
	mat2 r2 = mat2(vec2(0.3, 0.95), vec2(-0.95, 0.3));
	vec2 q1 = r1 * p;
	vec2 q2 = r2 * p;
	float f1 = vnoise(vec2(q1.x / fiber_length, q1.y / 1.2));
	float f2 = vnoise(vec2(q2.x / fiber_length, q2.y / 1.2));
	return smoothstep(0.6, 0.95, f1) + smoothstep(0.65, 0.95, f2);
}

// Hauteur totale de la surface du papier
float height(vec2 p) {
	float coarse = fbm(p / relief_size) * relief_amount;
	float tooth = fbm(p / grain_size) * grain_amount;
	float fib = fibers(p) * fiber_amount;
	return coarse + tooth + fib;
}

void fragment() {
	vec2 p = world_pos;
	float e = 1.0; // pas de dérivation, en pixels

	// Normale calculée à partir de la hauteur (différences centrées)
	float hx = height(p + vec2(e, 0.0)) - height(p - vec2(e, 0.0));
	float hy = height(p + vec2(0.0, e)) - height(p - vec2(0.0, e));
	vec3 n = normalize(vec3(-hx * bump_strength, -hy * bump_strength, 1.0));

	// Éclairage doux
	vec3 l = normalize(vec3(normalize(light_direction), light_height));
	float diff = dot(n, l) - l.z; // 0 sur une surface parfaitement plane

	vec3 col = paper_color.rgb * (1.0 + diff * light_strength);
	col = mix(col, shadow_color.rgb, clamp(-diff * light_strength * 1.5, 0.0, 1.0));

	// Léger vignettage
	float d = distance(UV, vec2(0.5));
	col *= 1.0 - smoothstep(0.35, 0.85, d) * vignette_strength;

	COLOR = vec4(col, paper_color.a);
}
