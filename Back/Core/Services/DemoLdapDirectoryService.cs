using System.Text.Json;
using DocumentPortalIam.Back.Core.Models;
using Microsoft.Extensions.Options;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class DemoLdapDirectoryService : IDirectoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _usersFile;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DemoLdapDirectoryService(IOptions<LdapDemoOptions> options, IWebHostEnvironment environment)
    {
        _usersFile = Path.Combine(environment.ContentRootPath, options.Value.UsersFile);
    }

    public async Task<AppUser?> AuthenticateAsync(string userName, string password)
    {
        var users = await GetUsersInternalAsync();
        return users.FirstOrDefault(user =>
            string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase)
            && user.Password == password);
    }

    public async Task<IReadOnlyList<AppUser>> GetUsersAsync()
    {
        var users = await GetUsersInternalAsync();
        return users
            .Select(CloneWithoutPassword)
            .OrderBy(user => user.UserName)
            .ToList();
    }

    public async Task UpdateRoleAsync(string userName, string role)
    {
        await _lock.WaitAsync();
        try
        {
            var users = await ReadUsersFromFileAsync();
            var user = users.FirstOrDefault(item =>
                string.Equals(item.UserName, userName, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                throw new InvalidOperationException("Usuario nao encontrado no diretorio LDAP demo.");
            }

            user.Roles = new List<string> { role };
            await WriteUsersToFileAsync(users);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<AppUser>> GetUsersInternalAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await ReadUsersFromFileAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<AppUser>> ReadUsersFromFileAsync()
    {
        if (!File.Exists(_usersFile))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_usersFile)!);
            await WriteUsersToFileAsync(SeedUsers());
        }

        await using var stream = File.OpenRead(_usersFile);
        return await JsonSerializer.DeserializeAsync<List<AppUser>>(stream) ?? new List<AppUser>();
    }

    private async Task WriteUsersToFileAsync(List<AppUser> users)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_usersFile)!);
        await using var stream = File.Create(_usersFile);
        await JsonSerializer.SerializeAsync(stream, users, JsonOptions);
    }

    private static AppUser CloneWithoutPassword(AppUser user) => new()
    {
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        Email = user.Email,
        Roles = user.Roles.ToList()
    };

    private static List<AppUser> SeedUsers() => new()
    {
        new AppUser
        {
            UserName = "admin",
            DisplayName = "Administrador IAM",
            Email = "admin@iam.local",
            Password = "Admin@123",
            Roles = new List<string> { AppRoles.Admin }
        },
        new AppUser
        {
            UserName = "gestor",
            DisplayName = "Gestor de Documentos",
            Email = "gestor@iam.local",
            Password = "Gestor@123",
            Roles = new List<string> { AppRoles.Manager }
        },
        new AppUser
        {
            UserName = "aluno",
            DisplayName = "Aluno / Usuario",
            Email = "aluno@iam.local",
            Password = "Aluno@123",
            Roles = new List<string> { AppRoles.User }
        },
        new AppUser
        {
            UserName = "auditor",
            DisplayName = "Auditor",
            Email = "auditor@iam.local",
            Password = "Auditor@123",
            Roles = new List<string> { AppRoles.Auditor }
        }
    };
}
