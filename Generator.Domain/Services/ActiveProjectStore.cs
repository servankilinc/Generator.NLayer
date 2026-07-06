using Generator.Domain.Core.Entities.Local;

namespace Generator.Domain.Services;

public class ActiveProjectStore : IActiveProjectStore
{
    private readonly object _lock = new();
    private Project? _activeProject;

    public Project? ActiveProject
    {
        get
        {
            lock (_lock) return _activeProject;
        }
        set
        {
            lock (_lock) _activeProject = value;
        }
    }
}
