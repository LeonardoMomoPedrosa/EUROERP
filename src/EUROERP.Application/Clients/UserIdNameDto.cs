namespace EUROERP.Application.Clients;

/// <summary>User id (Guid string) and name for dropdowns (e.g. sales agents).</summary>
public class UserIdNameDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
