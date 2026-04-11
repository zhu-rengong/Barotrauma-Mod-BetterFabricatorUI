using Barotrauma.LuaCs.Data;

namespace BetterFabricatorUI;

public partial class Plugin
{
    private static ISettingBase<bool>? _hasInitializedConfigurationSetting;
    public static bool HasInitializedConfiguration
    {
        get => _hasInitializedConfigurationSetting?.Value ?? false;
        set => _hasInitializedConfigurationSetting?.TrySetValue(value);
    }

    private void LoadConfig()
    {
        { _hasInitializedConfigurationSetting = ConfigService.TryGetConfig<ISettingBase<bool>>(_package, "HasInitializedConfiguration", out var val) ? val : null; }
        LoadConfigProjSpecific();
    }

    private partial void LoadConfigProjSpecific();
}