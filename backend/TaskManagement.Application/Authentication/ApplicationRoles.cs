namespace TaskManagement.Application.Authentication;

public static class ApplicationRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string ProjectManager = "ProjectManager";
    public const string Member = "Member";

    public static readonly string[] All = [SuperAdmin, Admin, ProjectManager, Member];
}
