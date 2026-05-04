using Microsoft.AspNetCore.Mvc;
using {{ core_project_name }}.Utils.Localization;
using {{ core_project_name }}.Enums;
using {{ webui_project_name }}.Utils.Extensions;
using {{ webui_project_name }}.Models.UI;

namespace {{ webui_project_name }}.ViewComponents;

public class HeaderViewComponent : ViewComponent
{
    private readonly LocalizationSettings _localizationSettings;

    public HeaderViewComponent(LocalizationSettings localizationSettings) => _localizationSettings = localizationSettings;

    public IViewComponentResult Invoke() {

        var currentUrl = HttpContext.GetUrl();
        var currentCulture = HttpContext.GetCurrentLanguage();
        
        var localizations = new List<LocalizationItem>
        {
            new LocalizationItem
            {
                Language = Language.Turkish,
                Name = Language.Turkish.ToString(),
                Culture = "tr-TR",
                Image = "/media/flags/turkey.svg",
                RedirectUrl = currentUrl
            },
            new LocalizationItem
            {
                Language = Language.English,
                Name = Language.English.ToString(),
                Culture = "en-US",
                Image = "/media/flags/united-states.svg",
                RedirectUrl = currentUrl
            },
            new LocalizationItem
            {
                Language = Language.Russian,
                Name = Language.Russian.ToString(),
                Culture = "ru-RU",
                Image = "/media/flags/russia.svg",
                RedirectUrl = currentUrl
            },
            new LocalizationItem
            {
                Language = Language.German,
                Name = Language.German.ToString(),
                Culture = "de-DE",
                Image = "/media/flags/germany.svg",
                RedirectUrl = currentUrl
            },
            new LocalizationItem
            {
                Language = Language.French,
                Name = Language.French.ToString(),
                Culture = "fr-FR",
                Image = "/media/flags/france.svg",
                RedirectUrl = currentUrl
            },
            new LocalizationItem
            {
                Language = Language.Spanish,
                Name = Language.Spanish.ToString(),
                Culture = "es-ES",
                Image = "/media/flags/spain.svg",
                RedirectUrl = currentUrl
            }
        };

        localizations = localizations
            .Where(loc => _localizationSettings.AvailableLanguages.Any(lang => lang == loc.Language))
            .ToList();

        foreach (var item in localizations) 
            if (item.Culture == currentCulture) 
                item.IsActive = true;
        
        return View(localizations);
    }
}
