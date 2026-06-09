using DocumentPortalIam.Back.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentPortalIam.Back.Core.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();
    public DbSet<AuditRecord> AuditLogs => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentRecord>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(document => document.StoredFileName).HasMaxLength(260).IsRequired();
            entity.Property(document => document.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(document => document.OwnerUserName).HasMaxLength(120).IsRequired();
            entity.Property(document => document.Sensitivity).HasMaxLength(60).IsRequired();
            entity.Property(document => document.UploadedAt).IsRequired();
        });

        modelBuilder.Entity<AuditRecord>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Timestamp).IsRequired();
            entity.Property(record => record.Action).HasMaxLength(120).IsRequired();
            entity.Property(record => record.Actor).HasMaxLength(120).IsRequired();
            entity.Property(record => record.Details).HasMaxLength(1000).IsRequired();
        });
    }
}
