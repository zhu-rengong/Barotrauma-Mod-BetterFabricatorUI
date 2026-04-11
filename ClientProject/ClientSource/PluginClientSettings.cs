using Barotrauma.LuaCs.Data;

namespace BetterFabricatorUI;

public partial class Plugin
{
    private static ISettingBase<bool>? _pinyinSearchEnabledSetting;
    public static bool PinyinSearchEnabled
    {
        get => _pinyinSearchEnabledSetting?.Value ?? false;
        set => _pinyinSearchEnabledSetting?.TrySetValue(value);
    }

    private partial void LoadConfigProjSpecific()
    {
        { _pinyinSearchEnabledSetting = ConfigService.TryGetConfig<ISettingBase<bool>>(_package, "PinyinSearchEnabled", out var val) ? val : null; }
    }
}