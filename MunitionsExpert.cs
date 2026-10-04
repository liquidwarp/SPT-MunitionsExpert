using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IcyClawz.MunitionsExpert;

internal enum ColorName
{
    Blue, Brown, Cyan, Gray, Green, Magenta, Orange, Pink, Purple, Red, Silver, Tan, Violet, Yellow
}

internal static class ColorCache
{
    private const byte ALPHA = 38;
    private static readonly Dictionary<ColorName, Color> Cache = new()
    {
        [ColorName.Blue] = new Color32(0, 60, 170, ALPHA),
        [ColorName.Brown] = new Color32(140, 85, 30, ALPHA),
        [ColorName.Cyan] = new Color32(0, 150, 150, ALPHA),
        [ColorName.Gray] = new Color32(70, 70, 70, ALPHA),
        [ColorName.Green] = new Color32(70, 140, 0, ALPHA),
        [ColorName.Magenta] = new Color32(215, 0, 100, ALPHA),
        [ColorName.Orange] = new Color32(140, 70, 0, ALPHA),
        [ColorName.Pink] = new Color32(215, 120, 150, ALPHA),
        [ColorName.Purple] = new Color32(120, 40, 135, ALPHA),
        [ColorName.Red] = new Color32(170, 20, 0, ALPHA),
        [ColorName.Silver] = new Color32(150, 150, 150, ALPHA),
        [ColorName.Tan] = new Color32(175, 145, 100, ALPHA),
        [ColorName.Violet] = new Color32(80, 50, 180, ALPHA),
        [ColorName.Yellow] = new Color32(170, 170, 0, ALPHA),
    };

    public static Color Get(ColorName name) => Cache.TryGetValue(name, out Color color) ? color : default;
}

internal enum EAmmoExtraAttributeId
{
    ArmorDamage, FragmentationChance, RicochetChance
}

internal static class IconCache
{
    private static readonly Dictionary<Enum, Sprite> Cache = new()
    {
        [EAmmoExtraAttributeId.ArmorDamage] = Load(nameof(EAmmoExtraAttributeId.ArmorDamage)),
        [EAmmoExtraAttributeId.FragmentationChance] = Load(nameof(EAmmoExtraAttributeId.FragmentationChance)),
        [EAmmoExtraAttributeId.RicochetChance] = Load(nameof(EAmmoExtraAttributeId.RicochetChance)),
    };

    public static Sprite Get(Enum id) => Cache.TryGetValue(id, out Sprite sprite) ? sprite : default;

    private static Sprite Load(string name)
    {
        using Stream stream = typeof(IconCache).Assembly
            .GetManifestResourceStream($"IcyClawz.MunitionsExpert.Resources.{name}.png");
        byte[] bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes); // resizes the texture to the PNG's own dimensions
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }
}

internal static class AmmoTemplateExtensions
{
    public static void AddExtraAttributes(this AmmoTemplate instance)
    {
        instance.SafelyAddQualityToList(new ItemAttribute(EAmmoExtraAttributeId.ArmorDamage)
        {
            Name = EAmmoExtraAttributeId.ArmorDamage.ToString(),
            DisplayNameFunc = () => "Armor damage",
            Base = () => instance.ArmorDamage,
            StringValue = () => $"{instance.ArmorDamage}%",
            Tooltip = () => Explosives.ArmorDamageTooltip,
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EItemAttributeId.DurabilityBurn)
        {
            Name = EItemAttributeId.DurabilityBurn.GetName(),
            Base = () => instance.DurabilityBurnModificator - 1f,
            StringValue = () => $"{(instance.DurabilityBurnModificator - 1f) * 100f:F1}%",
            DisplayType = () => EItemAttributeDisplayType.Compact,
            LabelVariations = EItemAttributeLabelVariations.Colored,
            LessIsGood = true,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EItemAttributeId.HeatFactor)
        {
            Name = EItemAttributeId.HeatFactor.GetName(),
            Base = () => instance.HeatFactor - 1f,
            StringValue = () => $"{(instance.HeatFactor - 1f) * 100f:F1}%",
            DisplayType = () => EItemAttributeDisplayType.Compact,
            LabelVariations = EItemAttributeLabelVariations.Colored,
            LessIsGood = true,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EAmmoExtraAttributeId.FragmentationChance)
        {
            Name = EAmmoExtraAttributeId.FragmentationChance.ToString(),
            DisplayNameFunc = () => "Fragmentation chance",
            Base = () => instance.FragmentationChance,
            StringValue = () => $"{instance.FragmentationChance * 100f:F1}%",
            Tooltip = () => "Indicative value, actual chance depends on the penetrated surface.",
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EAmmoExtraAttributeId.RicochetChance)
        {
            Name = EAmmoExtraAttributeId.RicochetChance.ToString(),
            DisplayNameFunc = () => "Ricochet chance",
            Base = () => instance.RicochetChance,
            StringValue = () => $"{(instance.RicochetChance * 100f):F1}%",
            Tooltip = () => "World surfaces only, no effect on armor. Indicative value, actual chance depends on surface and impact angle.",
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EItemAttributeId.MalfMisfireChance)
        {
            Name = EItemAttributeId.MalfMisfireChance.GetName(),
            Base = () => instance.MalfMisfireChance,
            StringValue = () =>
            {
                float maxMalfMisfireChance = AmmoTemplate.MaxMalfMisfireChance;
                int index = instance.MalfMisfireChance <= 0f ? 0
                    : instance.MalfMisfireChance < 3f * maxMalfMisfireChance / 7f ? 1
                    : instance.MalfMisfireChance < 4f * maxMalfMisfireChance / 7f ? 2
                    : instance.MalfMisfireChance < 5f * maxMalfMisfireChance / 7f ? 3
                    : instance.MalfMisfireChance < 6f * maxMalfMisfireChance / 7f ? 4
                    : 5;
                return AmmoTemplate.MalfChancesKeys[index].Localized();
            },
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EItemAttributeId.MalfFeedChance)
        {
            Name = EItemAttributeId.MalfFeedChance.GetName(),
            Base = () => instance.MalfFeedChance,
            StringValue = () =>
            {
                float maxMalfFeedChance = AmmoTemplate.MaxMalfFeedChance;
                int index = instance.MalfFeedChance <= 0f ? 0
                    : instance.MalfFeedChance < 1f * maxMalfFeedChance / 7f ? 1
                    : instance.MalfFeedChance < 3f * maxMalfFeedChance / 7f ? 2
                    : instance.MalfFeedChance < 5f * maxMalfFeedChance / 7f ? 3
                    : instance.MalfFeedChance < 6f * maxMalfFeedChance / 7f ? 4
                    : 5;
                return AmmoTemplate.MalfChancesKeys[index].Localized();
            },
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        if (instance.HasGrenaderComponent && instance.MaxExplosionDistance > 0f)
            instance.AddExplosiveAttributes();
    }

    private static void AddExplosiveAttributes(this AmmoTemplate instance)
    {
        Explosives.SetTooltip(instance._cachedQualities, EItemAttributeId.MaxAmmoDamage,
            "Direct hit only, the explosion damage is separate.");

        instance.SafelyAddQualityToList(new ItemAttribute(EItemAttributeId.MaximumThrowDamage)
        {
            Name = "BlastDamage",
            DisplayNameFunc = () => "Blast damage",
            Base = () => instance.ExplosionStrength,
            StringValue = () => instance.ExplosionStrength.ToString(),
            Tooltip = () => Explosives.BlastDamageTooltip,
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EItemAttributeId.ExplosionDistance)
        {
            Name = EItemAttributeId.ExplosionDistance.GetName(),
            Base = () => instance.MaxExplosionDistance,
            StringValue = () => $"{instance.MinExplosionDistance} - {instance.MaxExplosionDistance} {"meters".Localized()}",
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EItemAttributeId.ExplosionDelay)
        {
            Name = "FuzeArmTime",
            DisplayNameFunc = () => "Fuse arming",
            Base = () => instance.FuzeArmTimeSec,
            StringValue = () => $"{instance.FuzeArmTimeSec}s (~{instance.FuzeArmTimeSec * instance.InitialSpeed:F0}m)",
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        instance.SafelyAddQualityToList(new ItemAttribute(EItemAttributeId.FragmentsCount)
        {
            Name = EItemAttributeId.FragmentsCount.GetName(),
            Base = () => instance.FragmentsCount,
            StringValue = () => Explosives.FormatFragmentsCount(instance.FragmentsCount),
            Tooltip = () => Explosives.FragmentsCountTooltip,
            DisplayType = () => EItemAttributeDisplayType.Compact,
        });

        foreach (ItemAttribute attribute in Explosives.CreateFragmentAttributes(instance.FragmentsCount, instance.FragmentType))
            instance.SafelyAddQualityToList(attribute);
    }

    public static int GetPenetrationArmorClass(this AmmoTemplate instance)
    {
        var armorClasses = Singleton<GlobalConfiguration>.Instance.Armor.ArmorClass;
        for (int i = armorClasses.Length - 1; i >= 0; i--)
            if (armorClasses[i].Resistance <= instance.PenetrationPower)
                return i;
        return 0;
    }
}

internal static class ThrowWeapExtensions
{
    public static void AddExtraAttributes(this ThrowWeap instance)
    {
        Explosives.SetTooltip(instance.Attributes, EItemAttributeId.MaximumThrowDamage, Explosives.BlastDamageTooltip);
        Explosives.SetTooltip(instance.Attributes, EItemAttributeId.FragmentsCount, Explosives.FragmentsCountTooltip);
        foreach (ItemAttribute attribute in instance.Attributes)
            if (Equals(attribute.Id, EItemAttributeId.FragmentsCount))
                attribute.StringValue = () => Explosives.FormatFragmentsCount(instance.FragmentsCount);

        foreach (ItemAttribute attribute in Explosives.CreateFragmentAttributes(instance.FragmentsCount, instance.FragmentType))
            instance.SafelyAddAttributeToList(attribute);
    }
}

internal static class Explosives
{
    public const string BlastDamageTooltip = "Per exposed body part within the inner blast radius. Falls off with distance and cover.";
    public const string ArmorDamageTooltip = "Indicative value, actual durability loss also depends on penetration, armor class and material.";
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

        yield return new ItemAttribute(EItemAttributeId.MaxAmmoDamage)
        {
            Name = "FragmentDamage",
            DisplayNameFunc = () => "Fragment damage",
            Base = () => fragment.Damage,
            StringValue = () => fragment.Damage.ToString(),
            DisplayType = () => EItemAttributeDisplayType.Compact,
        };

        yield return new ItemAttribute(EItemAttributeId.AmmoPenetrationPower)
        {
            Name = "FragmentPenetrationPower",
            DisplayNameFunc = () => "Fragment penetration",
            Base = () => fragment.PenetrationPower,
            StringValue = () => fragment.PenetrationPower.ToString(),
            DisplayType = () => EItemAttributeDisplayType.Compact,
        };

        yield return new ItemAttribute(EAmmoExtraAttributeId.ArmorDamage)
        {
            Name = "FragmentArmorDamage",
            DisplayNameFunc = () => "Fragment armor damage",
            Base = () => fragment.ArmorDamage,
            StringValue = () => $"{fragment.ArmorDamage}%",
            Tooltip = () => ArmorDamageTooltip,
            DisplayType = () => EItemAttributeDisplayType.Compact,
        };
    }

    public static void SetTooltip(IEnumerable<ItemAttribute> attributes, EItemAttributeId id, string tooltip)
    {
        foreach (ItemAttribute attribute in attributes)
            if (Equals(attribute.Id, id))
                attribute.Tooltip = () => tooltip;
    }
}
