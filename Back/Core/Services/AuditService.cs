using DocumentPortalIam.Back.Core.Data;
using DocumentPortalIam.Back.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _database;

    public AuditService(AppDbContext database)
    {
        _database = database;
    }

    public async Task WriteAsync(string action, string actor, string details)
    {
        _database.AuditLogs.Add(new AuditRecord
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = action,
            Actor = string.IsNullOrWhiteSpace(actor) ? "anonymous" : actor,
            Details = details
        });

        await _database.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditRecord>> GetRecentAsync(int limit = 80)
    {
        var records = await _database.AuditLogs
            .AsNoTracking()
            .ToListAsync();

        return records
            .OrderByDescending(record => record.Timestamp)
            .Take(limit)
            .ToList();
    }
}
