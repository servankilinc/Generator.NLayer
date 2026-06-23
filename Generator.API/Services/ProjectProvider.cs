using Generator.Domain.Core.Entities.Local;
using Generator.Domain.Context;
using Generator.Domain.Services;

namespace Generator.API.Services;

public class ProjectProvider : IProjectProvider
{
    private readonly IActiveProjectStore _activeProjectStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Project? _currentProject;

    public ProjectProvider(IActiveProjectStore activeProjectStore, IHttpContextAccessor httpContextAccessor)
    {
        _activeProjectStore = activeProjectStore;
        _httpContextAccessor = httpContextAccessor;
    }

    public Project? CurrentProject
    {
        get
        {
            if (_currentProject != null)
                return _currentProject;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.Request.Headers.TryGetValue("X-Project-Id", out var headerValue) && 
                    int.TryParse(headerValue, out int projectId))
                {
                    using var localContext = new LocalContext();
                    _currentProject = localContext.Projects.FirstOrDefault(x => x.Id == projectId);
                }
            }

            _currentProject ??= _activeProjectStore.ActiveProject;
            return _currentProject;
        }
        set
        {
            _currentProject = value;
        }
    }
}
