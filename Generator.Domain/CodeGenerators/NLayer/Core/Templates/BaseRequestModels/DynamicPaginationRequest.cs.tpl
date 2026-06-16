using {{ core_project_name }}.Utils.DynamicQuery;
using {{ core_project_name }}.Utils.Pagination;

namespace {{ core_project_name }}.BaseRequestModels;

public class DynamicPaginationRequest : PaginationRequest
{
    public Filter? Filter { get; set; }
    public IEnumerable<Sort>? Sorts { get; set; }
}
