using {{ core_project_name }}.Enums;

namespace {{ core_project_name }}.Utils.Localization;

public class LocalizationSettings
{
    public Language DefaultLanguage { get; set; }
    public List<Language> AvailableLanugages { get; set; } = null!;
}

public class LocalizationSettingsConfigirationRaw
{
    public string? DefaultLanguage { get; set; }
    public List<string>? AvailableLanugages { get; set; }

    public LocalizationSettings ToLocalizationSettings() => new LocalizationSettings()
    {
        DefaultLanguage = GlobalExtensions.GetEnumByDescription<Language>(DefaultLanguage ?? Language.Turkish.GetDescription()),
        AvailableLanugages = AvailableLanugages?.Select(code => GlobalExtensions.GetEnumByDescription<Language>(code)).ToList() ?? [Language.Turkish]
    };
}
