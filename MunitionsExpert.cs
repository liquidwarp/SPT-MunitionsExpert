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

internal static class CompactAttribute
{
    public static ItemAttribute Create(Enum id, string name, Func<float> @base, Func<string> value,
        Func<string> displayName = null, Func<string> tooltip = null,
        EItemAttributeLabelVariations labelVariations = default, bool lessIsGood = false)
    {
        var attribute = new ItemAttribute(id)
        {
            Name = name,
            DisplayNameFunc = displayName,
            Base = @base,
            StringValue = value,
            DisplayType = () => EItemAttributeDisplayType.Compact,
            LabelVariations = labelVariations,
            LessIsGood = lessIsGood,
        };
        if (tooltip != null)
            attribute.Tooltip = tooltip;
        return attribute;
    }
}

internal static class AmmoTemplateExtensions
{
    public const string ArmorDamageTooltip = "Indicative value, actual durability loss also depends on penetration, armor class and material.";

    public static void AddExtraAttributes(this AmmoTemplate instance)
    {
        instance.SafelyAddQualityToList(CompactAttribute.Create(EAmmoExtraAttributeId.ArmorDamage,
            nameof(EAmmoExtraAttributeId.ArmorDamage),
            () => instance.ArmorDamage,
            () => $"{instance.ArmorDamage}%",
            displayName: () => "Armor damage",
            tooltip: () => ArmorDamageTooltip));

        if (instance.DurabilityBurnModificator > 0f)
            instance.SafelyAddQualityToList(CompactAttribute.Create(EItemAttributeId.DurabilityBurn,
                EItemAttributeId.DurabilityBurn.GetName(),
                () => instance.DurabilityBurnModificator - 1f,
                () => $"{(instance.DurabilityBurnModificator - 1f) * 100f:F1}%",
                labelVariations: EItemAttributeLabelVariations.Colored,
                lessIsGood: true));

        if (instance.HeatFactor > 0f)
            instance.SafelyAddQualityToList(CompactAttribute.Create(EItemAttributeId.HeatFactor,
                EItemAttributeId.HeatFactor.GetName(),
                () => instance.HeatFactor - 1f,
                () => $"{(instance.HeatFactor - 1f) * 100f:F1}%",
                labelVariations: EItemAttributeLabelVariations.Colored,
                lessIsGood: true));

        instance.SafelyAddQualityToList(CompactAttribute.Create(EAmmoExtraAttributeId.FragmentationChance,
            nameof(EAmmoExtraAttributeId.FragmentationChance),
            () => instance.FragmentationChance,
            () => Percent(instance.FragmentationChance),
            displayName: () => "Fragmentation chance",
            tooltip: () => "Indicative value, actual chance depends on the penetrated surface."));

        instance.SafelyAddQualityToList(CompactAttribute.Create(EAmmoExtraAttributeId.RicochetChance,
            nameof(EAmmoExtraAttributeId.RicochetChance),
            () => instance.RicochetChance,
            () => Percent(instance.RicochetChance),
            displayName: () => "Ricochet chance",
            tooltip: () => "World surfaces only, no effect on armor. Indicative value, actual chance depends on surface and impact angle."));

        instance.SafelyAddQualityToList(CompactAttribute.Create(EItemAttributeId.MalfMisfireChance,
            EItemAttributeId.MalfMisfireChance.GetName(),
            () => instance.MalfMisfireChance,
            () => MalfChance(instance.MalfMisfireChance, AmmoTemplate.MaxMalfMisfireChance, 3f, 4f, 5f, 6f)));

        instance.SafelyAddQualityToList(CompactAttribute.Create(EItemAttributeId.MalfFeedChance,
            EItemAttributeId.MalfFeedChance.GetName(),
            () => instance.MalfFeedChance,
            () => MalfChance(instance.MalfFeedChance, AmmoTemplate.MaxMalfFeedChance, 1f, 3f, 5f, 6f)));

        if (instance.HasGrenaderComponent && instance.MaxExplosionDistance > 0f)
            instance.AddExplosiveAttributes();
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

        instance.SafelyAddQualityToList(CompactAttribute.Create(EItemAttributeId.MaximumThrowDamage,
            "BlastDamage",
            () => instance.ExplosionStrength,
            () => instance.ExplosionStrength.ToString(),
            displayName: () => "Blast damage",
            tooltip: () => Explosives.BlastDamageTooltip));

        instance.SafelyAddQualityToList(CompactAttribute.Create(EItemAttributeId.ExplosionDistance,
            EItemAttributeId.ExplosionDistance.GetName(),
            () => instance.MaxExplosionDistance,
            () => $"{instance.MinExplosionDistance} - {instance.MaxExplosionDistance} {"meters".Localized()}"));

        if (instance.FuzeArmTimeSec > 0f)
            instance.SafelyAddQualityToList(CompactAttribute.Create(EItemAttributeId.ExplosionDelay,
                "FuzeArmTime",
                () => instance.FuzeArmTimeSec,
                () => $"{instance.FuzeArmTimeSec}s (~{instance.FuzeArmTimeSec * instance.InitialSpeed:F0}m)",
                displayName: () => "Fuse arming"));

        instance.SafelyAddQualityToList(CompactAttribute.Create(EItemAttributeId.FragmentsCount,
            EItemAttributeId.FragmentsCount.GetName(),
            () => instance.FragmentsCount,
            () => Explosives.FormatFragmentsCount(instance.FragmentsCount),
            tooltip: () => Explosives.FragmentsCountTooltip));

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
                attribute.Tooltip = () => Explosives.BlastDamageTooltip;
            }
            else if (id == EItemAttributeId.FragmentsCount)
            {
                attribute.StringValue = () => Explosives.FormatFragmentsCount(instance.FragmentsCount);
                attribute.Tooltip = () => Explosives.FragmentsCountTooltip;
            }
        }

        foreach (ItemAttribute attribute in Explosives.CreateFragmentAttributes(instance.FragmentsCount, instance.FragmentType))
            instance.SafelyAddAttributeToList(attribute);
    }
}

internal static class Explosives
{
    public const string BlastDamageTooltip = "Per exposed body part within the inner blast radius. Falls off with distance and cover.";
    public const string FragmentsCountTooltip = "Indicative value, the game fires at most 30 fragments per explosion.";

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

        yield return CompactAttribute.Create(EItemAttributeId.MaxAmmoDamage, "FragmentDamage",
            () => fragment.Damage,
            () => fragment.Damage.ToString(),
            displayName: () => "Fragment damage");

        yield return CompactAttribute.Create(EItemAttributeId.AmmoPenetrationPower, "FragmentPenetrationPower",
            () => fragment.PenetrationPower,
            () => fragment.PenetrationPower.ToString(),
            displayName: () => "Fragment penetration");

        yield return CompactAttribute.Create(EAmmoExtraAttributeId.ArmorDamage, "FragmentArmorDamage",
            () => fragment.ArmorDamage,
            () => $"{fragment.ArmorDamage}%",
            displayName: () => "Fragment armor damage",
            tooltip: () => AmmoTemplateExtensions.ArmorDamageTooltip);
    }

    public static void SetTooltip(List<ItemAttribute> attributes, EItemAttributeId id, Func<string> tooltip)
    {
        foreach (ItemAttribute attribute in attributes)
            if (attribute.Id is EItemAttributeId attributeId && attributeId == id)
                attribute.Tooltip = tooltip;
    }
}
