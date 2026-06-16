using System.ComponentModel;

namespace {{ core_project_name }}.Enums;

public enum AuthenticationType : byte
{
    [Description("None")]
    None = 0,
    [Description("Email")]
    Email = 1,
    [Description("Google")]
    Google = 2,
    [Description("Facebook")]
    Facebook = 3,
}
