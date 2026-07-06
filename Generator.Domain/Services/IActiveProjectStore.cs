using Generator.Domain.Core.Entities.Local;

namespace Generator.Domain.Services;

public interface IActiveProjectStore
{
    Project? ActiveProject { get; set; }
}
