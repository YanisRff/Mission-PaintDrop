shader_type canvas_item;
// Mur = coup de pinceau de peinture posé sur le papier (Godot 4.x).
// Conçu pour BaseBlock : un mur en forme de capsule (bouts complètement arrondis),
// dessiné par un quad centré sur l'origine et un peu plus grand que le mur.
// La taille du mur est déduite de ce quad : un seul matériau pour tous les murs.
//
// Les stries suivent le sens du trait (le grand côté du mur). Les côtés sont à peine
// irréguliers, les bouts s'effilochent comme le début et la fin d'un coup de pinceau.

// Marge du quad autour du mur, en px : doit être égale à BaseBlock.Margin
const float MARGIN = 16.0;

group_uniforms paper;
// Doit être identique au "grain_size" du shader du papier
uniform float grain_size : hint_range(0.5, 8.0) = 2.0;
// Le papier affleure légèrement dans la peinture (0 = peinture parfaitement pleine)
uniform float paper_show : hint_range(0.0, 1.0) = 0.2;

group_uniforms brush;
uniform float streak_length = 70.0;   // longueur des stries (px)
uniform float streak_width = 3.0;     // épaisseur des poils du pinceau (px)
uniform float streak_contrast : hint_range(0.0, 1.0) = 0.25; // stries claires / sombres
uniform float side_fray = 2.0;        // irrégularité des côtés (px)
uniform float end_fray = 6.0;         // effilochage des bouts (px)
uniform float edge_pool : hint_range(0.0, 1.0) = 0.25; // peinture plus dense le long des bords
uniform float pool_width = 5.0;       // largeur de cette bordure (px)

varying vec2 local_pos;
varying vec2 world_pos;
varying vec2 origin;
varying vec2 half_size;

void vertex() {
	local_pos = VERTEX;
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; // grain fixe sur le papier
	origin = MODEL_MATRIX[3].xy;                            // chaque mur a ses propres stries
	// Les 4 coins du quad ont la même valeur absolue : demi-taille du mur
	half_size = abs(VERTEX) - vec2(MARGIN);
}

// ----- Bruit (identique à celui du shader du papier) -----
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
	vec2 h = half_size;
	bool horizontal = h.x >= h.y;
	float r = max(min(h.x, h.y), 1.0);   // rayon des bouts = moitié du petit côté
	float half_long = max(h.x, h.y);

	// ----- Capsule : distance signée au contour, en px (négatif = dedans) -----
	vec2 p = local_pos;
	vec2 q = abs(p) - (h - vec2(r));
	float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;

	// ----- Repère du pinceau : "along" le long du trait, "across" en travers -----
	float along_pos = horizontal ? p.x : p.y;
	vec2 sp = (horizontal ? p : p.yx) + origin;

	// Poils du pinceau : bruit étiré dans le sens du trait
	vec2 s = vec2(sp.x / streak_length, sp.y / streak_width);
	float streak = vnoise(s) * 0.65 + vnoise(s * vec2(2.3, 2.1) + 5.7) * 0.35;

	// ----- Contour : quasi net sur les côtés, effiloché aux bouts -----
	float cap = clamp((abs(along_pos) - (half_long - r)) / r, 0.0, 1.0); // 0 = flanc, 1 = pointe
	float fray = mix(side_fray, end_fray, cap * cap);
	float inset = min(fray, r * 0.3) * streak;
	float e = -d - inset; // distance au contour irrégulier, positive à l'intérieur

	float aa = max(fwidth(d), 0.0001);
	float cover = smoothstep(-aa, aa, e);

	// ----- Papier qui affleure dans la peinture -----
	float grain = fbm(world_pos / grain_size);
	cover *= 1.0 - paper_show * smoothstep(0.5, 0.75, mix(grain, streak, 0.5));

	// ----- Couleur -----
	vec3 base = COLOR.rgb;
	vec3 col = base;

	// Stries claires / sombres dans le sens du pinceau
	col *= 1.0 + (streak - 0.5) * streak_contrast;
	// La peinture épouse le grain du papier
	col *= 1.0 + (grain - 0.5) * 0.2;
	// Épaisseur de peinture qui varie doucement sur la longueur
	col *= 0.92 + 0.16 * vnoise(sp / 50.0);

	// Peinture plus dense le long du contour
	float pool = 1.0 - smoothstep(0.0, pool_width, e);
	col = mix(col, base * 0.55, pool * edge_pool);

	COLOR = vec4(col, cover * COLOR.a);
}
