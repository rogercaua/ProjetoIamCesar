using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class LdapOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 389;
    public bool UseSsl { get; set; }
    public string BaseDn { get; set; } = "dc=example,dc=org";
    public string UsersOu { get; set; } = "ou=users";
    public string GroupsOu { get; set; } = "ou=groups";
    public string AdminDn { get; set; } = "";
    public string AdminPassword { get; set; } = "";
    public string UserSearchFilter { get; set; } = "(uid={0})";
    public string UserNameAttribute { get; set; } = "uid";
    public string DisplayNameAttribute { get; set; } = "cn";
    public string EmailAttribute { get; set; } = "mail";
    public string GroupNameAttribute { get; set; } = "cn";
    public string GroupMemberAttribute { get; set; } = "member";
    public string DefaultRole { get; set; } = AppRoles.User;
    public Dictionary<string, string> RoleMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> RoleGroupDns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
