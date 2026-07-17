namespace TaskManagement.Api.Services;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string ProjectManager = "ProjectManager";
    public const string Member = "Member";

    public static readonly string[] All = [Admin, ProjectManager, Member];
}
