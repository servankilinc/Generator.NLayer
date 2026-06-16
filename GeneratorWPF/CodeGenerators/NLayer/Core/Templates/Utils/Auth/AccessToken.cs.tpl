namespace {{ core_project_name }}.Utils.Auth;

public record AccessToken(string Token, DateTime Expiration);