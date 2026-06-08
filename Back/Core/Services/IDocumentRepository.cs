using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public interface IDocumentRepository
{
    Task<DocumentRecord> SaveAsync(IFormFile file, string ownerUserName, string sensitivity);
    Task<IReadOnlyList<DocumentRecord>> GetAllAsync();
    Task<DocumentRecord?> FindAsync(int id);
    Task<Stream> OpenReadAsync(DocumentRecord document);
    string GetStoredPath(DocumentRecord document);
    Task DeleteAsync(DocumentRecord document);
}
