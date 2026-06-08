namespace DocumentPortalIam.Back.Core.Dtos;

public sealed class RoleDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}

public sealed class UpdateRoleRequestDto
{
    public string Role { get; set; } = "";
}
