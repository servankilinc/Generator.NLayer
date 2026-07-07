using Generator.Domain.Core.Entities.Local;

namespace Generator.Domain.Core;

public static class Statics
{
    public static HashSet<string> nonReferanceTypes = new()
    {
        "int", "long", "float", "double", "bool", "char", "byte", "Guid" // "DateTime", "DateOnly", "TimeOnly"
    };

    public static bool IsReferanceTypeNullable(string type)
    {
        if (type == "DateTime" || type == "DateOnly" || type == "TimeOnly")
            return false;
        return true;
    }


    public const string IEntity = "IEntity";
    public const string ISoftDeletableEntity = "ISoftDeletableEntity";
    public const string IArchivableEntity = "IArchivableEntity";
    public const string IAuditableEntity = "IAuditableEntity";
}
