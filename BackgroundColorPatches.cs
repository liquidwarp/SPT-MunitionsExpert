using System.Reflection;
using EFT.HandBook;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using SPT.Reflection.Patching;
using UnityEngine;

namespace Liquidwarp.MunitionsExpert;

internal class ItemViewPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(ItemView).GetMethod(nameof(ItemView.UpdateColor), BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    private static void PatchPrefix(ItemView __instance, ref Color ___BackgroundColor)
    {
        if (AmmoBackgroundColor.TryGet(__instance.Item, out Color color))
            ___BackgroundColor = color;
    }
}

internal class EntityIconPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(EntityIcon).GetMethod(nameof(EntityIcon.Show), BindingFlags.Public | BindingFlags.Instance);

    [PatchPostfix]
    private static void PatchPostfix(EntityIcon __instance, Item item)
    {
        if (AmmoBackgroundColor.TryGet(item, out Color color))
            __instance._colorPanel.color = color;
    }
}
