shader_type canvas_item;
// Goutte de peinture qui frotte sur le papier (Godot 4.x).
// La couleur vient du Modulate du nœud (ColorComponent / TintParent) : on part de COLOR.rgb.
//
// Au repos : goutte nette et brillante, au bord à peine rugueux (le grain du papier).
// En mouvement : l'avant reste rond et l'arrière s'allonge en une traîne qui s'affine,
// dans la direction exacte de la vitesse (horizontale, verticale ou diagonale).
// Le reflet s'estompe : la peinture devient plus mate.

// Renseignés par Player.cs
uniform float radius = 16.0;
uniform vec2 velocity = vec2(0.0);
// Vitesse de référence par axe (course, saut) : l'effet est maximal à ces vitesses
uniform vec2 speed_reference = vec2(220.0, 420.0);
uniform float squash : hint_range(0.0, 1.0) = 0.0; // écrasement à l'atterrissage

group_uniforms paper;
// Doit être identique au "grain_size" du shader du papier : la peinture
// s'accroche alors exactement aux mêmes creux que le papier.
uniform float grain_size : hint_range(0.5, 8.0) = 2.0;
uniform float edge_grain : hint_range(0.0, 0.2) = 0.06; // bord rugueux, même à l'arrêt

group_uniforms drag;
uniform float tail_length : hint_range(0.0, 2.0) = 1.0;  // longueur de la traîne
uniform float tail_taper : hint_range(0.0, 1.5) = 0.9;   // finesse de la traîne

group_uniforms look;
uniform vec2 light_direction = vec2(-0.45, -0.6);
uniform float shade_strength : hint_range(0.0, 1.0) = 0.4;
uniform float edge_darkness : hint_range(0.0, 1.0) = 0.5;
uniform float highlight_strength : hint_range(0.0, 1.0) = 0.85;
uniform float highlight_sharpness : hint_range(5.0, 120.0) = 45.0;
uniform float sheen_strength : hint_range(0.0, 1.0) = 0.2;

varying vec2 local_pos;
varying vec2 world_pos;

void vertex() {
	local_pos = VERTEX;                                  // centré sur le joueur
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; // fixe sur le papier
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

// Écrasement à l'atterrissage + goutte "tirée" : traîne derrière, avant arrondi
vec2 deform(vec2 p, vec2 dir, vec2 perp, float s) {
	// Écrasement : plus large, plus bas, le bas reste posé
	float k = squash * 0.3;
	p.y -= 1.0 - 1.0 / (1.0 + k);
	p.x /= 1.0 + k;
	p.y *= 1.0 + k;

	// Traîne : seul l'arrière (opposé à la vitesse) s'allonge et s'affine
	float a = dot(p, dir);
	float b = dot(p, perp);
	float back = clamp(-a, 0.0, 1.0);
	back *= back;
	a = a < 0.0 ? a / (1.0 + 1.2 * s) : a / (1.0 + 0.15 * s);
	b *= 1.0 + tail_taper * s * back;
	return dir * a + perp * b;
}

void fragment() {
	vec2 p = local_pos / radius;

	// ----- Direction et intensité du mouvement -----
	// La direction suit la vitesse réelle (saut, chute, diagonale...).
	// L'intensité est mesurée par axe, pour qu'un saut étire autant qu'une course.
	float speed = length(velocity);
	vec2 dir = speed > 1.0 ? velocity / speed : vec2(1.0, 0.0);
	vec2 perp = vec2(-dir.y, dir.x);
	float sp = clamp(length(velocity / speed_reference), 0.0, 1.0);

	// ----- Forme -----
	vec2 q = deform(p, dir, perp, sp * tail_length);
	float d = length(q) - 1.0;
	float aa = max(fwidth(d), 0.0001);

	// ----- Texture du papier, fixe dans le monde -----
	vec2 wp = world_pos;
	float grain = fbm(wp / grain_size);

	// ----- Bord : légèrement rugueux, comme la peinture sur le grain du papier -----
	float body = 1.0 - smoothstep(-aa, aa, d + (grain - 0.5) * 2.0 * edge_grain);

	// ----- Normale d'un dôme bombé -----
	vec2 c = q / max(length(q), 1.0);
	float z = sqrt(max(1.0 - dot(c, c), 0.0));
	vec3 n = normalize(vec3(c * 0.85, max(z, 0.2)));

	// ----- Éclairage -----
	vec3 l = normalize(vec3(light_direction, 0.8));
	float light = dot(n, l) * 0.5 + 0.5;
	vec3 base = COLOR.rgb;

	vec3 col = base * mix(1.0 - shade_strength, 1.0 + shade_strength * 0.3, light);

	// Bords plus épais / plus foncés
	float rim = pow(1.0 - n.z, 2.0);
	col = mix(col, base * 0.45, rim * edge_darkness);

	// La peinture épouse le grain du papier
	col *= 1.0 + (grain - 0.5) * 0.15;

	// Reflet humide, qui s'estompe quand la peinture est tirée sur le papier
	vec3 h = normalize(l + vec3(0.0, 0.0, 1.0));
	float ndh = max(dot(n, h), 0.0);
	float wet = 1.0 - 0.7 * sp;
	float hard = smoothstep(0.35, 0.55, pow(ndh, highlight_sharpness)) * highlight_strength * wet;
	float soft = pow(ndh, 8.0) * sheen_strength * wet;
	col = mix(col, vec3(1.0), clamp(hard + soft, 0.0, 1.0));

	COLOR = vec4(col, body * COLOR.a);
}
