using System.Text.Json;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class AuditService : IAuditService
{
    private readonly string _auditFile;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AuditService(IWebHostEnvironment environment)
    {
        _auditFile = Path.Combine(environment.ContentRootPath, "Storage", "audit.log");
    }

    public async Task WriteAsync(string action, string actor, string details)
    {
        var record = new AuditRecord
        {
            Action = action,
            Actor = string.IsNullOrWhiteSpace(actor) ? "anonymous" : actor,
            Details = details
        };

        await _lock.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_auditFile)!);
            await File.AppendAllTextAsync(_auditFile, JsonSerializer.Serialize(record) + Environment.NewLine);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<AuditRecord>> GetRecentAsync(int limit = 80)
    {
        if (!File.Exists(_auditFile))
        {
            return Array.Empty<AuditRecord>();
        }

        var lines = await File.ReadAllLinesAsync(_auditFile);
        return lines
            .Reverse()
            .Take(limit)
            .Select(line =>
            {
                try
                {
                    return JsonSerializer.Deserialize<AuditRecord>(line);
                }
                catch
                {
                    return null;
                }
            })
            .Where(record => record is not null)
            .Cast<AuditRecord>()
            .ToList();
    }
}
