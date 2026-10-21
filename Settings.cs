using BepInEx.Configuration;
using UnityEngine;

namespace Liquidwarp.MunitionsExpert;

internal static class Settings
{
    private const string ColorsSection = "Ammo Background Colors";
    private const string StatsSection = "Ammo Inspect Stats";

    private static int _order = 1000;

    public static ConfigEntry<bool> ColorizeBackgrounds { get; private set; }
    private static ConfigEntry<Color>[] ArmorClassColors { get; set; }

    public static ConfigEntry<bool> ArmorDamage { get; private set; }
    public static ConfigEntry<bool> DurabilityBurn { get; private set; }
    public static ConfigEntry<bool> HeatFactor { get; private set; }
    public static ConfigEntry<bool> FragmentationChance { get; private set; }
    public static ConfigEntry<bool> RicochetChance { get; private set; }
    public static ConfigEntry<bool> MisfireChance { get; private set; }
    public static ConfigEntry<bool> FeedFailureChance { get; private set; }
    public static ConfigEntry<bool> BlastDamage { get; private set; }
    public static ConfigEntry<bool> ExplosionRadius { get; private set; }
    public static ConfigEntry<bool> FuseArming { get; private set; }
    public static ConfigEntry<bool> FragmentCount { get; private set; }
    public static ConfigEntry<bool> FragmentStats { get; private set; }

    public static void Init(ConfigFile config)
    {
        ColorizeBackgrounds = Bind(config, ColorsSection, "Enabled", true,
            "Color ammo icon backgrounds by armor class penetration.");
        ArmorClassColors = [
            BindColor(config, "Unarmored", new Color32(120, 40, 135, 38), "< 10"),
            BindColor(config, "Class 1", new Color32(0, 60, 170, 38), "< 20"),
            BindColor(config, "Class 2", new Color32(0, 150, 150, 38), "< 30"),
            BindColor(config, "Class 3", new Color32(70, 140, 0, 38), "< 40"),
            BindColor(config, "Class 4", new Color32(170, 170, 0, 38), "< 50"),
            BindColor(config, "Class 5", new Color32(140, 70, 0, 38), "< 60"),
            BindColor(config, "Class 6+", new Color32(170, 20, 0, 38), ">= 60"),
        ];

        ArmorDamage = BindStat(config, "Armor damage", "Armor damage percentage.");
        DurabilityBurn = BindStat(config, "Durability burn", "Weapon durability burn modifier.");
        HeatFactor = BindStat(config, "Heat", "Weapon heat modifier.");
        FragmentationChance = BindStat(config, "Fragmentation chance", "Chance for the bullet to fragment.");
        RicochetChance = BindStat(config, "Ricochet chance", "Chance for the bullet to ricochet off world surfaces.");
        MisfireChance = BindStat(config, "Misfire chance", "Misfire malfunction chance.");
        FeedFailureChance = BindStat(config, "Feed failure chance", "Feed failure malfunction chance.");
        BlastDamage = BindStat(config, "Blast damage", "Explosion damage of explosive rounds.");
        ExplosionRadius = BindStat(config, "Explosion radius", "Explosion radius of explosive rounds.");
        FuseArming = BindStat(config, "Fuse arming", "Fuse arming time and distance of explosive rounds.");
        FragmentCount = BindStat(config, "Fragment count", "Fragment count of explosive rounds.");
        FragmentStats = BindStat(config, "Fragment stats", "Fragment damage, penetration and armor damage of explosive rounds.");
    }

    public static Color GetArmorClassColor(int armorClass) =>
        ArmorClassColors[Mathf.Clamp(armorClass, 0, ArmorClassColors.Length - 1)].Value;

    private static ConfigEntry<Color> BindColor(ConfigFile config, string key, Color defaultValue, string penetrates) =>
        Bind(config, ColorsSection, key, defaultValue, $"Background color of ammo with penetration {penetrates}.");

    private static ConfigEntry<bool> BindStat(ConfigFile config, string key, string description)
    {
        ConfigEntry<bool> entry = Bind(config, StatsSection, key, true, $"Show {char.ToLowerInvariant(description[0])}{description.Substring(1)}");
        entry.SettingChanged += (_, _) => AmmoTemplateExtensions.RefreshAllExtraAttributes();
        return entry;
    }

    private static ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue, string description) =>
        config.Bind(section, key, defaultValue,
            new ConfigDescription(description, null, new ConfigurationManagerAttributes { Order = _order-- }));
}
