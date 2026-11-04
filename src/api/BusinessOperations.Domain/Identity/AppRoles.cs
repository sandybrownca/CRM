namespace BusinessOperations.Domain.Identity;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Operations = "Operations";

    public static readonly IReadOnlyList<string> All = [Administrator, Operations];
}
