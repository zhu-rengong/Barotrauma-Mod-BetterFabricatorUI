using Barotrauma.LuaCs.Data;
using System.Diagnostics.CodeAnalysis;

namespace BetterFabricatorUI;

public partial class Plugin
{
    private static ISettingBase<bool> _hasInitializedConfigurationSetting = null!;
    public static bool HasInitializedConfiguration
    {
        get => _hasInitializedConfigurationSetting.Value;
        set => _hasInitializedConfigurationSetting.TrySetValue(value);
    }

    private void LoadConfig()
    {
        TryGetConfig("HasInitializedConfiguration", out _hasInitializedConfigurationSetting);
        LoadConfigProjSpecific();

        if (!HasInitializedConfiguration)
        {
            if (GameSettings.CurrentConfig.Language == "Simplified Chinese".ToLanguageIdentifier()
                || GameSettings.CurrentConfig.Language == "Traditional Chinese".ToLanguageIdentifier())
            {
                PinyinSearchEnabled = true;
                ConfigService.SaveConfigValue(_pinyinSearchEnabledSetting);
            }

            HasInitializedConfiguration = true;
            ConfigService.SaveConfigValue(_hasInitializedConfigurationSetting);
        }
    }

    private partial void LoadConfigProjSpecific();

    private bool TryGetConfig<T>(string name, [NotNullWhen(true)] out T setting) where T : ISettingBase
    {
        if (!ConfigService.TryGetConfig(_package, name, out setting))
        {
            LoggerService.LogError($"Failed to find config named {name}!");
            return false;
        }

        return true;
    }
}