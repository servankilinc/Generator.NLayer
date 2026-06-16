using {{ core_project_name }}.Utils.DynamicQuery;

namespace {{ core_project_name }}.BaseRequestModels;

public class DynamicRequest
{
    public Filter? Filter { get; set; }
    public IEnumerable<Sort>? Sorts { get; set; }
}
