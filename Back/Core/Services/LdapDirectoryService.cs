using System.Text;
using DocumentPortalIam.Back.Core.Models;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class LdapDirectoryService : IDirectoryService
{
    private readonly LdapOptions _options;

    public LdapDirectoryService(IOptions<LdapOptions> options)
    {
        _options = options.Value;
    }

    public async Task<AppUser?> AuthenticateAsync(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        try
        {
            var foundUser = await FindUserAsync(userName);
            if (foundUser is null)
            {
                return null;
            }

            using var userConnection = await CreateConnectionAsync();
            await userConnection.BindAsync(foundUser.DistinguishedName, password, CancellationToken.None);
            return foundUser.User;
        }
        catch (LdapException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<AppUser>> GetUsersAsync()
    {
        using var connection = await CreateConnectionAsync();
        await BindAdminAsync(connection);

        var results = await connection.SearchAsync(
            BuildDn(_options.UsersOu),
            LdapConnection.ScopeSub,
            $"({_options.UserNameAttribute}=*)",
            GetUserAttributes(),
            false,
            CancellationToken.None);

        var users = new List<AppUser>();
        while (await results.HasMoreAsync(CancellationToken.None))
        {
            var entry = await results.NextAsync(CancellationToken.None);
            users.Add(await MapUserEntryAsync(connection, entry));
        }

        return users.OrderBy(user => user.UserName).ToList();
    }

    public async Task UpdateRoleAsync(string userName, string role)
    {
        var foundUser = await FindUserAsync(userName)
            ?? throw new InvalidOperationException("Usuario nao encontrado no LDAP.");

        using var connection = await CreateConnectionAsync();
        await BindAdminAsync(connection);

        var roleGroups = GetRoleGroupDns();
        if (!roleGroups.TryGetValue(role, out var targetGroupDn))
        {
            throw new InvalidOperationException("Papel sem grupo LDAP configurado.");
        }

        await TryModifyGroupAsync(connection, targetGroupDn, LdapModification.Add, foundUser.DistinguishedName);

        foreach (var groupDn in roleGroups.Values
            .Where(groupDn => !string.Equals(groupDn, targetGroupDn, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await TryModifyGroupAsync(connection, groupDn, LdapModification.Delete, foundUser.DistinguishedName);
        }
    }

    private async Task<FoundLdapUser?> FindUserAsync(string userName)
    {
        using var connection = await CreateConnectionAsync();
        await BindAdminAsync(connection);

        var filter = string.Format(_options.UserSearchFilter, EscapeLdapFilterValue(userName));
        var results = await connection.SearchAsync(
            BuildDn(_options.UsersOu),
            LdapConnection.ScopeSub,
            filter,
            GetUserAttributes(),
            false,
            CancellationToken.None);

        if (!await results.HasMoreAsync(CancellationToken.None))
        {
            return null;
        }

        var entry = await results.NextAsync(CancellationToken.None);
        return new FoundLdapUser(entry.Dn, await MapUserEntryAsync(connection, entry));
    }

    private async Task<AppUser> MapUserEntryAsync(LdapConnection connection, LdapEntry entry)
    {
        var roles = (await ReadRolesForUserAsync(connection, entry))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roles.Count == 0)
        {
            roles.Add(_options.DefaultRole);
        }

        return new AppUser
        {
            UserName = GetAttributeValue(entry, _options.UserNameAttribute),
            DisplayName = GetAttributeValue(entry, _options.DisplayNameAttribute),
            Email = GetAttributeValue(entry, _options.EmailAttribute),
            Roles = roles
        };
    }

    private async Task<IReadOnlyList<string>> ReadRolesForUserAsync(LdapConnection connection, LdapEntry userEntry)
    {
        var userDn = userEntry.Dn;
        var userName = GetAttributeValue(userEntry, _options.UserNameAttribute);
        var memberValue = EscapeLdapFilterValue(userDn);
        var memberUidValue = EscapeLdapFilterValue(userName);

        var filter = $"(|({_options.GroupMemberAttribute}={memberValue})(uniqueMember={memberValue})(memberUid={memberUidValue}))";
        var results = await connection.SearchAsync(
            BuildDn(_options.GroupsOu),
            LdapConnection.ScopeSub,
            filter,
            new[] { _options.GroupNameAttribute },
            false,
            CancellationToken.None);

        var roles = new List<string>();
        while (await results.HasMoreAsync(CancellationToken.None))
        {
            var group = await results.NextAsync(CancellationToken.None);
            var groupName = GetAttributeValue(group, _options.GroupNameAttribute);
            roles.Add(MapGroupToRole(groupName, group.Dn));
        }

        return roles;
    }

    private string MapGroupToRole(string groupName, string groupDn)
    {
        if (_options.RoleMappings.TryGetValue(groupDn, out var byDn))
        {
            return byDn;
        }

        if (_options.RoleMappings.TryGetValue(groupName, out var byName))
        {
            return byName;
        }

        return groupName switch
        {
            "Administradores" => AppRoles.Admin,
            "Gestores" => AppRoles.Manager,
            "Usuarios" => AppRoles.User,
            "Auditores" => AppRoles.Auditor,
            _ => _options.DefaultRole
        };
    }

    private IReadOnlyDictionary<string, string> GetRoleGroupDns()
    {
        if (_options.RoleGroupDns.Count > 0)
        {
            return _options.RoleGroupDns;
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoles.Admin] = BuildDn("cn=Administradores", _options.GroupsOu),
            [AppRoles.Manager] = BuildDn("cn=Gestores", _options.GroupsOu),
            [AppRoles.User] = BuildDn("cn=Usuarios", _options.GroupsOu),
            [AppRoles.Auditor] = BuildDn("cn=Auditores", _options.GroupsOu)
        };
    }

    private async Task TryModifyGroupAsync(LdapConnection connection, string groupDn, int operation, string userDn)
    {
        try
        {
            var attribute = new LdapAttribute(_options.GroupMemberAttribute, userDn);
            var modification = new LdapModification(operation, attribute);
            await connection.ModifyAsync(groupDn, modification, CancellationToken.None);
        }
        catch (LdapException)
        {
            // Ignora membros ausentes ou ja existentes para manter a troca de papel simples.
        }
    }

    private async Task<LdapConnection> CreateConnectionAsync()
    {
        var connection = new LdapConnection
        {
            SecureSocketLayer = _options.UseSsl
        };

        await connection.ConnectAsync(_options.Host, _options.Port, CancellationToken.None);
        return connection;
    }

    private async Task BindAdminAsync(LdapConnection connection)
    {
        await connection.BindAsync(_options.AdminDn, _options.AdminPassword, CancellationToken.None);
    }

    private string[] GetUserAttributes()
    {
        return new[]
        {
            _options.UserNameAttribute,
            _options.DisplayNameAttribute,
            _options.EmailAttribute
        }.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private string BuildDn(params string[] parts)
    {
        var cleanParts = parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim())
            .ToList();

        if (cleanParts.Count > 0 && cleanParts[^1].Contains("dc=", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(",", cleanParts);
        }

        cleanParts.Add(_options.BaseDn);
        return string.Join(",", cleanParts);
    }

    private static string GetAttributeValue(LdapEntry entry, string attributeName)
    {
        return entry.GetAttributeSet().GetAttribute(attributeName)?.StringValue ?? "";
    }

    private static string EscapeLdapFilterValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '\\' => "\\5c",
                '*' => "\\2a",
                '(' => "\\28",
                ')' => "\\29",
                '\0' => "\\00",
                _ => character
            });
        }

        return builder.ToString();
    }

    private sealed record FoundLdapUser(string DistinguishedName, AppUser User);
}
