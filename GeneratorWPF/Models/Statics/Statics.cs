namespace GeneratorWPF.Models.Statics;
public static class Statics
{
    public static HashSet<string> nonReferanceTypes = new()
    {
        "int", "long", "float", "double", "bool", "char", "byte", "DateTime", "DateOnly", "Guid"
    };

    public const string IEntity = "IEntity";
    public const string ISoftDeletableEntity = "ISoftDeletableEntity";
    public const string IArchivableEntity = "IArchivableEntity";
    public const string IAuditableEntity = "IAuditableEntity";
}
