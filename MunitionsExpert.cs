using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Liquidwarp.MunitionsExpert;

internal enum EAmmoExtraAttributeId
{
    ArmorDamage, FragmentationChance, RicochetChance
}

internal static class IconCache
{
    private static readonly Dictionary<Enum, Sprite> Cache = LoadAll();

    public static Sprite Get(Enum id) => Cache.TryGetValue(id, out Sprite sprite) ? sprite : null;

    private static Dictionary<Enum, Sprite> LoadAll()
    {
        var cache = new Dictionary<Enum, Sprite>();
        foreach (EAmmoExtraAttributeId id in Enum.GetValues(typeof(EAmmoExtraAttributeId)))
            cache[id] = Load(id.ToString());
        return cache;
    }

    private static Sprite Load(string name)
    {
        using Stream stream = typeof(IconCache).Assembly.GetManifestResourceStream($"{name}.png");
        byte[] bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }
}

internal static class AmmoBackgroundColor
{
    public static bool TryGet(Item item, out Color color)
    {
        color = default;
        if (!Settings.ColorizeBackgrounds.Value || item is not Ammo ammo || ammo.PenetrationPower <= 0)
            return false;
        color = Settings.GetArmorClassColor(ammo.AmmoTemplate.GetPenetrationArmorClass());
        return true;
    }
}

internal static class CompactAttribute
{
    public static ItemAttribute Create(ConfigEntry<bool> setting, Enum id, string name, Func<float> @base, Func<string> value,
        Func<string> displayName = null, Func<string> tooltip = null,
        EItemAttributeLabelVariations labelVariations = default, bool lessIsGood = false)
    {
        var attribute = new ItemAttribute(id)
        {
            Name = name,
            DisplayNameFunc = displayName,
            Base = @base,
            StringValue = value,
            DisplayType = () => setting.Value ? EItemAttributeDisplayType.Compact : EItemAttributeDisplayType.Special,
            LabelVariations = labelVariations,
            LessIsGood = lessIsGood,
        };
        if (tooltip != null)
            attribute.Tooltip = tooltip;
        return attribute;
    }
}

internal static class Tooltips
{
    public const string ArmorDamage = "Indicative value, actual durability loss also depends on penetration, armor class and material.";
    public const string FragmentationChance = "Indicative value, actual chance depends on the penetrated surface.";
    public const string RicochetChance = "World surfaces only, no effect on armor. Indicative value, actual chance depends on surface and impact angle.";
    public const string BlastDamage = "Per exposed body part within the inner blast radius. Falls off with distance and cover.";
    public const string FragmentsCount = "Indicative value, the game fires at most 30 fragments per explosion.";
}

internal static class AmmoTemplateExtensions
{
    public static void AddExtraAttributes(this AmmoTemplate instance)
    {
        instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.ArmorDamage, EAmmoExtraAttributeId.ArmorDamage,
            nameof(EAmmoExtraAttributeId.ArmorDamage),
            () => instance.ArmorDamage,
            () => $"{instance.ArmorDamage}%",
            displayName: () => "Armor damage",
            tooltip: () => Tooltips.ArmorDamage));

        if (instance.DurabilityBurnModificator > 0f)
            instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.DurabilityBurn, EItemAttributeId.DurabilityBurn,
                EItemAttributeId.DurabilityBurn.GetName(),
                () => instance.DurabilityBurnModificator - 1f,
                () => $"{(instance.DurabilityBurnModificator - 1f) * 100f:F1}%",
                labelVariations: EItemAttributeLabelVariations.Colored,
                lessIsGood: true));

        if (instance.HeatFactor > 0f)
            instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.HeatFactor, EItemAttributeId.HeatFactor,
                EItemAttributeId.HeatFactor.GetName(),
                () => instance.HeatFactor - 1f,
                () => $"{(instance.HeatFactor - 1f) * 100f:F1}%",
                labelVariations: EItemAttributeLabelVariations.Colored,
                lessIsGood: true));

        instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.FragmentationChance, EAmmoExtraAttributeId.FragmentationChance,
            nameof(EAmmoExtraAttributeId.FragmentationChance),
            () => instance.FragmentationChance,
            () => Percent(instance.FragmentationChance),
            displayName: () => "Fragmentation chance",
            tooltip: () => Tooltips.FragmentationChance));

        instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.RicochetChance, EAmmoExtraAttributeId.RicochetChance,
            nameof(EAmmoExtraAttributeId.RicochetChance),
            () => instance.RicochetChance,
            () => Percent(instance.RicochetChance),
            displayName: () => "Ricochet chance",
            tooltip: () => Tooltips.RicochetChance));

        instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.MisfireChance, EItemAttributeId.MalfMisfireChance,
            EItemAttributeId.MalfMisfireChance.GetName(),
            () => instance.MalfMisfireChance,
            () => MalfChance(instance.MalfMisfireChance, AmmoTemplate.MaxMalfMisfireChance, 3f, 4f, 5f, 6f)));

        instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.FeedFailureChance, EItemAttributeId.MalfFeedChance,
            EItemAttributeId.MalfFeedChance.GetName(),
            () => instance.MalfFeedChance,
            () => MalfChance(instance.MalfFeedChance, AmmoTemplate.MaxMalfFeedChance, 1f, 3f, 5f, 6f)));

        if (instance.HasGrenaderComponent && instance.MaxExplosionDistance > 0f)
            instance.AddExplosiveAttributes();
    }

    public static int GetPenetrationArmorClass(this AmmoTemplate instance)
    {
        var armorClasses = Singleton<GlobalConfiguration>.Instance.Armor.ArmorClass;
        for (int i = armorClasses.Length - 1; i >= 0; i--)
            if (armorClasses[i].Resistance <= instance.PenetrationPower)
                return i;
        return 0;
    }

    private static string Percent(float chance) => $"{Mathf.Clamp01(chance) * 100f:F1}%";

    private static string MalfChance(float chance, float max, float bound1, float bound2, float bound3, float bound4)
    {
        int index = chance <= 0f ? 0
            : chance < bound1 * max / 7f ? 1
            : chance < bound2 * max / 7f ? 2
            : chance < bound3 * max / 7f ? 3
            : chance < bound4 * max / 7f ? 4
            : 5;
        return AmmoTemplate.MalfChancesKeys[index].Localized();
    }

    private static void AddExplosiveAttributes(this AmmoTemplate instance)
    {
        Explosives.SetTooltip(instance._cachedQualities, EItemAttributeId.MaxAmmoDamage,
            () => "Direct hit only, the explosion damage is separate.");

        instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.BlastDamage, EItemAttributeId.MaximumThrowDamage,
            "BlastDamage",
            () => instance.ExplosionStrength,
            () => instance.ExplosionStrength.ToString(),
            displayName: () => "Blast damage",
            tooltip: () => Tooltips.BlastDamage));

        instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.ExplosionRadius, EItemAttributeId.ExplosionDistance,
            EItemAttributeId.ExplosionDistance.GetName(),
            () => instance.MaxExplosionDistance,
            () => $"{instance.MinExplosionDistance} - {instance.MaxExplosionDistance} {"meters".Localized()}"));

        if (instance.FuzeArmTimeSec > 0f)
            instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.FuseArming, EItemAttributeId.ExplosionDelay,
                "FuzeArmTime",
                () => instance.FuzeArmTimeSec,
                () => $"{instance.FuzeArmTimeSec}s (~{instance.FuzeArmTimeSec * instance.InitialSpeed:F0}m)",
                displayName: () => "Fuse arming"));

        instance.SafelyAddQualityToList(CompactAttribute.Create(Settings.FragmentCount, EItemAttributeId.FragmentsCount,
            EItemAttributeId.FragmentsCount.GetName(),
            () => instance.FragmentsCount,
            () => Explosives.FormatFragmentsCount(instance.FragmentsCount),
            tooltip: () => Tooltips.FragmentsCount));

        foreach (ItemAttribute attribute in Explosives.CreateFragmentAttributes(instance.FragmentsCount, instance.FragmentType))
            instance.SafelyAddQualityToList(attribute);
    }
}

internal static class ThrowWeapExtensions
{
    public static void AddExtraAttributes(this ThrowWeap instance)
    {
        foreach (ItemAttribute attribute in instance.Attributes)
        {
            if (attribute.Id is not EItemAttributeId id)
                continue;
            if (id == EItemAttributeId.MaximumThrowDamage)
            {
                attribute.Tooltip = () => Tooltips.BlastDamage;
            }
            else if (id == EItemAttributeId.FragmentsCount)
            {
                attribute.StringValue = () => Explosives.FormatFragmentsCount(instance.FragmentsCount);
                attribute.Tooltip = () => Tooltips.FragmentsCount;
            }
        }

        foreach (ItemAttribute attribute in Explosives.CreateFragmentAttributes(instance.FragmentsCount, instance.FragmentType))
            instance.SafelyAddAttributeToList(attribute);
    }
}

internal static class Explosives
{
    // The game caps fragments fired per explosion at 30, whatever the template lists.
    private const int MaxFiredFragments = 30;

    public static string FormatFragmentsCount(int count) =>
        count > MaxFiredFragments ? $"{MaxFiredFragments} ({count})" : count.ToString();

    public static IEnumerable<ItemAttribute> CreateFragmentAttributes(int fragmentsCount, string fragmentType)
    {
        if (fragmentsCount <= 0 || string.IsNullOrEmpty(fragmentType)
            || !Singleton<ItemFactory>.Instance.ItemTemplates.TryGetValue(fragmentType, out ItemTemplate template)
            || template is not AmmoTemplate fragment)
            yield break;

        yield return CompactAttribute.Create(Settings.FragmentDamage, EItemAttributeId.MaxAmmoDamage, "FragmentDamage",
            () => fragment.Damage,
            () => fragment.Damage.ToString(),
            displayName: () => "Fragment damage");

        yield return CompactAttribute.Create(Settings.FragmentPenetration, EItemAttributeId.AmmoPenetrationPower, "FragmentPenetrationPower",
            () => fragment.PenetrationPower,
            () => fragment.PenetrationPower.ToString(),
            displayName: () => "Fragment penetration");

        yield return CompactAttribute.Create(Settings.FragmentArmorDamage, EAmmoExtraAttributeId.ArmorDamage, "FragmentArmorDamage",
            () => fragment.ArmorDamage,
            () => $"{fragment.ArmorDamage}%",
            displayName: () => "Fragment armor damage",
            tooltip: () => Tooltips.ArmorDamage);
    }

    public static void SetTooltip(List<ItemAttribute> attributes, EItemAttributeId id, Func<string> tooltip)
    {
        foreach (ItemAttribute attribute in attributes)
            if (attribute.Id is EItemAttributeId attributeId && attributeId == id)
                attribute.Tooltip = tooltip;
    }
}
