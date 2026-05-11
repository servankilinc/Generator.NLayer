namespace Generator.Domain.Core;

public static class Enums
{
    public enum CrudTypeEnums
    {
        Read = 1,
        Create = 2,
        Update = 3,
        Delete = 4,
    }

    public enum DeleteBehaviorTypeEnums
    {
        Cascade = 1,
        ClientCascade = 2,
        Restrict = 3,
        ClientSetNull = 4,
        ClientNoAction = 5,
        SetNull = 6,
        NoAction = 7
    }

    public enum FieldTypeEnums
    {
        Int = 1,
        String = 2,
        Long = 3,
        Float = 4,
        Double = 5,
        Bool = 6,
        Char = 7,
        Byte = 8,
        DateTime = 9,
        DateOnly = 10,
        Guid = 11,
        TimeSpan = 12,
        TimeOnly = 13
    }

    public enum FieldTypeSourceEnums
    {
        Base = 1,
        Entity = 2,
        Dto = 3,
    }

    public enum RelationTypeEnums
    {
        OneToOne = 1,
        OneToMany = 2,
        //ManyToMany = 3
    }

    public enum ServiceLayerEnums
    {
        Core = 1,
        Model = 2,
        DataAccess = 3,
        Business = 4,
        Presentation = 5,
    }

    public enum ValidatorTypeParams
    {
        NotEqual_Value = 1,
        MaxLength_Max = 2,
        Range_Min = 3,
        Range_Max = 4,
        MinLength_Min = 5,
        Regex_Pattern = 6,
        GreaterThan_Value = 7,
        LessThan_Value = 8,
        Length_Value = 9
    }

    public enum ValidatorTypes
    {
        NotEmpty = 1,
        NotNull = 2,
        NotEqual = 3,
        MaxLength = 4,
        Range = 5,
        MinLength = 6,
        Regex = 7,
        GreaterThan = 8,
        LessThan = 9,
        EmailAddress = 10,
        CreditCard = 11,
        Phone = 12,
        Url = 13,
        Date = 14,
        Number = 15,
        GuidNotEmpty = 16,
        Length = 17,
    }
}
