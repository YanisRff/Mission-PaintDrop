shader_type canvas_item;
// Traînée de peinture (Godot 4.x), à utiliser avec PaintTrail.cs.
// Au lieu de simplement devenir transparente, la peinture sèche :
// en vieillissant elle ne reste plus que dans les creux du grain du papier
// et dans quelques stries, puis disparaît. Les bords du ruban sont effilochés.
//
// PaintTrail fournit : COLOR.rgb = couleur, COLOR.a = fraîcheur (1 -> 0),
// UV.x = distance parcourue le long du ruban (px), UV.y = position en travers (-1 à 1).

uniform float opacity : hint_range(0.0, 1.0) = 0.9;
// Doit être identique au "grain_size" du shader du papier
uniform float grain_size : hint_range(0.5, 8.0) = 2.0;
uniform float dryness : hint_range(0.0, 1.0) = 0.85;   // vitesse à laquelle la peinture "décroche"
uniform float edge_fray : hint_range(0.0, 1.0) = 0.5;  // bords effilochés
uniform float streak_length = 60.0;                    // longueur des stries (px)
uniform float streak_count : hint_range(1.0, 20.0) = 6.0; // nombre de stries sur la largeur

varying vec2 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

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

void fragment() {
	float life = COLOR.a;

	float grain = fbm(world_pos / grain_size);
	vec2 s = vec2(UV.x / streak_length, UV.y * streak_count);
	float streak = vnoise(s) * 0.65 + vnoise(s * vec2(2.3, 2.1) + 5.7) * 0.35;
	float tex = mix(grain, streak, 0.55);

	// Plus la peinture est vieille, moins elle accroche
	float threshold = tex * dryness;
	float cover = smoothstep(threshold - 0.1, threshold + 0.1, life);
	cover *= smoothstep(0.0, 0.08, life);

	// Bords effilochés
	float e = abs(UV.y) + (streak - 0.5) * edge_fray;
	cover *= 1.0 - smoothstep(0.7, 1.0, e);

	vec3 col = COLOR.rgb * (1.0 + (streak - 0.5) * 0.25);
	COLOR = vec4(col, cover * opacity);
}
