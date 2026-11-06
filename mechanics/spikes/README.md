# Système de Piques (Spikes) avec Couleurs

Ce dossier contient l'implémentation du système de piques répétables **intégrées au système de couleurs du jeu**.

## 🎯 Comportement

Les piques suivent **exactement le même système de collision que les murs colorés** :

- **Si la pique est de couleur C** et le joueur aussi : le joueur **passe à travers** (selon sa règle `BlockedByOtherColors`)
- **Si la pique est de couleur C** et le joueur a une autre couleur : le joueur **meurt** et le niveau est reseté

Cela permet de créer des niveaux où le joueur doit changer de couleur pour passer à travers certaines piques.

## 📁 Structure

- **`Spike.tscn`** + **`Spike.cs`** : Une seule pique colorée, avec détection intelligente
- **`SpikesRow.tscn`** + **`SpikesRow.cs`** : Générateur de ligne de piques répétées

## 🎨 Utilisation

### Pour une seule pique colorée
1. Ajouter la scène `Spike.tscn` à votre niveau
2. Dans l'inspecteur :
   - Assigner **Paint** → choisir une couleur (ex: `res://core/colors/red.tres`)
   - La pique sera automatiquement sur le layer de cette couleur
3. La pique tue le joueur **uniquement s'il est bloqué par cette couleur**

### Pour une ligne de piques colorées
1. Ajouter la scène `SpikesRow.tscn` à votre niveau
2. Dans l'inspecteur :
   - Assigner **SpikeScene** → `res://mechanics/spikes/Spike.tscn`
   - Assigner **Paint** → la couleur souhaitée pour toutes les piques
   - Définir **RowWidth** (largeur totale de la ligne, ex: 200)
   - Définir **SpikeWidth** (largeur d'une seule pique, par défaut 20)
3. Les piques se génèrent automatiquement quand vous ajustez RowWidth

## 🔧 Fonctionnement technique

### Détection intelligente
Chaque pique utilise `Area2D` pour détecter les joueurs, puis vérifie :

1. Récupère le `ColorComponent` du joueur
2. Applique la règle du joueur (`BlockedByOtherColors` ou `BlockedByOwnColor`)
3. Si le joueur **devrait être bloqué** par cette couleur → **reset du niveau**

### Configuration des layers
- **CollisionLayer** : `Paint.LayerMask` (layer 3=black, 4=red, 5=blue, 6=yellow, etc.)
- **CollisionMask** : `PhysicsLayers.PlayerMask` (détecte tous les joueurs)

### Intégration avec le système existant
- Utilise `LevelManager.Instance.RestartCurrentLevel()` pour reset le niveau
- Respecte les mêmes règles que `ColorComponent` et `BaseBlock`

## 🎨 Personnalisation

### Changer la couleur
- Assigner une autre `PaintColor` dans l'inspecteur (ex: `res://core/colors/blue.tres`)

### Changer l'apparence
- Modifier le `Polygon2D` dans `Spike.tscn`
- Ou remplacer par un `Sprite2D` avec votre propre texture

### Changer la taille de collision
- Modifier le `CollisionShape2D` dans `Spike.tscn`

## 💡 Astuces

- Pour créer une ligne de piques de différentes couleurs, créez plusieurs `SpikesRow` avec des couleurs différentes
- Les piques rouges tueront les joueurs bleus/noirs/jaunes, mais laisseront passer les joueurs rouges
- Combinez avec le système de changement de couleur (`Brush`) pour créer des puzzles complexes
