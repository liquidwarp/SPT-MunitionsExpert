using System;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;
using UnityEngine;

namespace Liquidwarp.MunitionsExpert;

internal class StaticIconsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(StaticIcons).GetMethod(nameof(StaticIcons.GetAttributeIcon), BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    private static bool PatchPrefix(Enum id) =>
        IconCache.Get(id) == null;

    [PatchPostfix]
    private static void PatchPostfix(ref Sprite __result, Enum id)
    {
        Sprite sprite = IconCache.Get(id);
        if (sprite != null)
            __result = sprite;
    }
}

internal class AmmoTemplatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(AmmoTemplate).GetMethod(nameof(AmmoTemplate.GetCachedReadonlyQualities), BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    private static void PatchPrefix(AmmoTemplate __instance, out bool __state) =>
        __state = __instance._cachedQualities == null;

    [PatchPostfix]
    private static void PatchPostfix(AmmoTemplate __instance, bool __state)
    {
        if (__state)
            __instance.AddExtraAttributes();
    }
}

internal class ThrowWeapPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(ThrowWeap).GetConstructor([typeof(string), typeof(ThrowWeapTemplate)]);

    [PatchPostfix]
    private static void PatchPostfix(ThrowWeap __instance) =>
        __instance.AddExtraAttributes();
}
