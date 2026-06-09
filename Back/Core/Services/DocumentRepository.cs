using DocumentPortalIam.Back.Core.Data;
using DocumentPortalIam.Back.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class DocumentRepository : IDocumentRepository
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private readonly AppDbContext _database;
    private readonly string _documentsPath;

    public DocumentRepository(AppDbContext database, IWebHostEnvironment environment)
    {
        _database = database;
        _documentsPath = Path.Combine(environment.ContentRootPath, "Storage", "Documents");
    }

    public async Task<DocumentRecord> SaveAsync(IFormFile file, string ownerUserName, string sensitivity)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("O limite do projeto e 10 MB.");
        }

        Directory.CreateDirectory(_documentsPath);

        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var record = new DocumentRecord
        {
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeInBytes = file.Length,
            OwnerUserName = ownerUserName,
            Sensitivity = string.IsNullOrWhiteSpace(sensitivity) ? "Interno" : sensitivity,
            UploadedAt = DateTimeOffset.UtcNow
        };

        await using (var destination = File.Create(GetStoredPath(record)))
        {
            await file.CopyToAsync(destination);
        }

        _database.Documents.Add(record);
        await _database.SaveChangesAsync();
        return record;
    }

    public async Task<IReadOnlyList<DocumentRecord>> GetAllAsync()
    {
        var documents = await _database.Documents
            .AsNoTracking()
            .ToListAsync();

        return documents
            .OrderByDescending(document => document.UploadedAt)
            .ToList();
    }

    public async Task<DocumentRecord?> FindAsync(int id)
    {
        return await _database.Documents.FindAsync(id);
    }

    public Task<Stream> OpenReadAsync(DocumentRecord document)
    {
        Stream stream = File.OpenRead(GetStoredPath(document));
        return Task.FromResult(stream);
    }

    public string GetStoredPath(DocumentRecord document) => Path.Combine(_documentsPath, document.StoredFileName);

    public async Task DeleteAsync(DocumentRecord document)
    {
        _database.Documents.Remove(document);
        await _database.SaveChangesAsync();

        var file = GetStoredPath(document);
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }
}
