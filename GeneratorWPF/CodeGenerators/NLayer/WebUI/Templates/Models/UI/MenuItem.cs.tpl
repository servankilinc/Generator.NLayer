namespace {{ webui_project_name }}.Models.UI;

public class MenuItem
{
    public string Title { get; set; } = "Menu Item";
    public string? Path { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Target { get; set; } = "_self"; // _self, _blank, _parent, _top
    public bool IsActive { get; set; }
    public bool HasActiveChild { get; set; }
    public List<MenuItem>? SubMenuItems { get; set; }
}
