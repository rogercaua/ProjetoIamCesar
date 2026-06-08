using System.Text.Json;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class DocumentRepository : IDocumentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _documentsPath;
    private readonly string _indexFile;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DocumentRepository(IWebHostEnvironment environment)
    {
        var storageRoot = Path.Combine(environment.ContentRootPath, "Storage");
        _documentsPath = Path.Combine(storageRoot, "documents");
        _indexFile = Path.Combine(storageRoot, "documents.json");
    }

    public async Task<DocumentRecord> SaveAsync(IFormFile file, string ownerUserName, string sensitivity)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new InvalidOperationException("O limite do demo e 10 MB.");
        }

        await _lock.WaitAsync();
        try
        {
            var documents = await ReadIndexAsync();
            var nextId = documents.Count == 0 ? 1 : documents.Max(document => document.Id) + 1;
            var extension = Path.GetExtension(file.FileName);
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var record = new DocumentRecord
            {
                Id = nextId,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                OwnerUserName = ownerUserName,
                Sensitivity = sensitivity,
                UploadedAt = DateTimeOffset.UtcNow
            };

            Directory.CreateDirectory(_documentsPath);
            await using (var destination = File.Create(GetStoredPath(record)))
            {
                await file.CopyToAsync(destination);
            }

            documents.Add(record);
            await WriteIndexAsync(documents);
            return record;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<DocumentRecord>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return (await ReadIndexAsync())
                .OrderByDescending(document => document.UploadedAt)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<DocumentRecord?> FindAsync(int id)
    {
        var documents = await GetAllAsync();
        return documents.FirstOrDefault(document => document.Id == id);
    }

    public Task<Stream> OpenReadAsync(DocumentRecord document)
    {
        Stream stream = File.OpenRead(GetStoredPath(document));
        return Task.FromResult(stream);
    }

    public string GetStoredPath(DocumentRecord document) => Path.Combine(_documentsPath, document.StoredFileName);

    public async Task DeleteAsync(DocumentRecord document)
    {
        await _lock.WaitAsync();
        try
        {
            var documents = await ReadIndexAsync();
            documents.RemoveAll(item => item.Id == document.Id);
            await WriteIndexAsync(documents);

            var file = GetStoredPath(document);
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<DocumentRecord>> ReadIndexAsync()
    {
        if (!File.Exists(_indexFile))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_indexFile)!);
            await WriteIndexAsync(new List<DocumentRecord>());
        }

        await using var stream = File.OpenRead(_indexFile);
        return await JsonSerializer.DeserializeAsync<List<DocumentRecord>>(stream) ?? new List<DocumentRecord>();
    }

    private async Task WriteIndexAsync(List<DocumentRecord> documents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_indexFile)!);
        await using var stream = File.Create(_indexFile);
        await JsonSerializer.SerializeAsync(stream, documents, JsonOptions);
    }
}
