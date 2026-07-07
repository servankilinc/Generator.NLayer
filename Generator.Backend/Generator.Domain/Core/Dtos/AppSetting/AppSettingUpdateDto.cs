namespace Generator.Domain.Core.Dtos.AppSetting;

public class AppSettingUpdateDto
{
    public int Id { get; set; }
    public string? ProjectName { get; set; }
    public string? SolutionName { get; set; }
    public string? Path { get; set; }
    public string? DBConnectionString { get; set; }

    public bool IsThereIdentity { get; set; }

    public bool IsThereUser { get; set; }
    public int? UserEntityId { get; set; }
    public bool IsThereRole { get; set; }
    public int? RoleEntityId { get; set; }

    public void MapToEntity(Entities.AppSetting appSetting)
    {
        appSetting.ProjectName = ProjectName;
        appSetting.SolutionName = SolutionName;
        appSetting.Path = Path;
        appSetting.DBConnectionString = DBConnectionString;
        appSetting.IsThereIdentity = IsThereIdentity;
        appSetting.IsThereUser = IsThereUser;
        appSetting.UserEntityId = UserEntityId;
        appSetting.IsThereRole = IsThereRole;
        appSetting.RoleEntityId = RoleEntityId;
    }
}
