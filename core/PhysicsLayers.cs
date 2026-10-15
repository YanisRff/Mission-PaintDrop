// core/PhysicsLayers.cs
public static class PhysicsLayers
{
    // Numéros de layers (1 à 32), uniquement ceux qui ne sont PAS des couleurs
    public const int World = 1;
    public const int Player = 2;
    public const int FirstLayerColor = 3;

    // Bitmasks correspondants
    public const uint WorldMask = 1u << (World - 1);
    public const uint PlayerMask = 1u << (Player - 1);

    // Convertit un numéro de layer (1-32) en bitmask
    public static uint ToMask(int layer) => 1u << (layer - 1);
}