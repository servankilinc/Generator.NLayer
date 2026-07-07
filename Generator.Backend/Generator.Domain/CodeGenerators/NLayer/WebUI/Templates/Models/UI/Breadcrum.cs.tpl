namespace {{ webui_project_name }}.Models.UI;

public class Breadcrum
{
    public string? Title { get; set; }
    public string PageName { get; set; } = "Page";
    public List<BreadcrumbItem>? BreadcrumbItems { get; set; }
}

public class BreadcrumbItem
{
    public string Title { get; set; } = "Page";
    public string? Path { get; set; }
    public string Icon { get; set; } = string.Empty;
}