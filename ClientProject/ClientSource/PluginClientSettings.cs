using Barotrauma.LuaCs.Data;

namespace BetterFabricatorUI;

public partial class Plugin
{
    private static ISettingBase<bool> _pinyinSearchEnabledSetting = null!;
    public static bool PinyinSearchEnabled
    {
        get => _pinyinSearchEnabledSetting.Value;
        set => _pinyinSearchEnabledSetting.TrySetValue(value);
    }

    private partial void LoadConfigProjSpecific()
    {
        TryGetConfig("PinyinSearchEnabled", out _pinyinSearchEnabledSetting);
    }
}