using AuditService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditTrail>(entity =>
        {
            entity.ToTable("audit_trail");

            entity.HasKey(x => x.AuditId);

            entity.Property(x => x.AuditId)
                .HasColumnName("audit_id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.TableName)
                .HasColumnName("table_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.RecordId)
                .HasColumnName("record_id")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Action)
                .HasColumnName("action")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(x => x.OldValues)
                .HasColumnName("old_values")
                .HasColumnType("jsonb");

            entity.Property(x => x.NewValues)
                .HasColumnName("new_values")
                .HasColumnType("jsonb");

            entity.Property(x => x.ChangedBy)
                .HasColumnName("changed_by")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.ChangedAt)
                .HasColumnName("changed_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(x => x.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45);

            entity.Property(x => x.AppContext)
                .HasColumnName("app_context")
                .HasColumnType("jsonb");
        });
    }
}