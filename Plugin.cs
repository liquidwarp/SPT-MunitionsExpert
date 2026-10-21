using BepInEx;

namespace Liquidwarp.MunitionsExpert;

[BepInPlugin(MyModInfo.MOD_GUID, MyModInfo.MOD_NAME, MyModInfo.MOD_VERSION)]
[BepInDependency("com.SPT.custom", MyModInfo.CLIENT_SPT_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        Settings.Init(Config);
        new StaticIconsPatch().Enable();
        new AmmoTemplatePatch().Enable();
        new ThrowWeapPatch().Enable();
        new ItemViewPatch().Enable();
        new EntityIconPatch().Enable();
    }
}
