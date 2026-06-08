using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public interface IDirectoryService
{
    Task<AppUser?> AuthenticateAsync(string userName, string password);
    Task<IReadOnlyList<AppUser>> GetUsersAsync();
    Task UpdateRoleAsync(string userName, string role);
}
