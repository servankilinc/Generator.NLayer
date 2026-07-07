using {{ core_project_name }}.Enums;

namespace {{ webui_project_name }}.Models.UI;

public class LocalizationItem
{
    public Language Language { get; set; }
    public string Name { get; set; } = null!;
    public string Culture { get; set; } = null!;
    public string Image { get; set; } = null!;
    public string? RedirectUrl { get; set; }
    public bool IsActive { get; set; }
}
