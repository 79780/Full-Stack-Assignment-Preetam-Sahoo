using IpiPro.Api.Domain;
using IpiPro.Api.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace IpiPro.Api.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Lab> Labs => Set<Lab>();
    public DbSet<Manifest> Manifests => Set<Manifest>();
    public DbSet<Specimen> Specimens => Set<Specimen>();
    public DbSet<Discrepancy> Discrepancies => Set<Discrepancy>();

    /// <summary>
    /// Referenced by the global query filters below. EF turns this into a query parameter
    /// that is re-read on every execution, so one cached query shape serves every tenant
    /// while the value stays request-scoped. The 0 fallback matches no row on purpose:
    /// a missing tenant returns nothing rather than everything.
    /// </summary>
    public int CurrentLabId => _tenant.LabId ?? 0;

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Lab>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        b.Entity<Manifest>(e =>
        {
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.ClinicName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

            e.HasOne(x => x.Lab).WithMany(l => l.Manifests)
                .HasForeignKey(x => x.LabId).OnDelete(DeleteBehavior.Cascade);

            // Manifest codes are only unique inside a lab. Two labs may legitimately
            // both run an MF-1001.
            e.HasIndex(x => new { x.LabId, x.Code }).IsUnique();

            e.HasQueryFilter(x => x.LabId == CurrentLabId);
        });

        b.Entity<Specimen>(e =>
        {
            e.Property(x => x.Code).IsRequired().HasMaxLength(50);
            e.Property(x => x.Patient).IsRequired().HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

            e.HasOne(x => x.Manifest).WithMany(m => m.Specimens)
                .HasForeignKey(x => x.ManifestId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.LabId, x.Code }).IsUnique();
            e.HasIndex(x => x.ManifestId);

            e.HasQueryFilter(x => x.LabId == CurrentLabId);
        });

        b.Entity<Discrepancy>(e =>
        {
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

            e.HasOne(x => x.Manifest).WithMany(m => m.Discrepancies)
                .HasForeignKey(x => x.ManifestId).OnDelete(DeleteBehavior.Cascade);

            // Restrict, not Cascade: a specimen row is never deleted in this flow, and two
            // cascade paths into the same table is a footgun the moment we move to SQL Server.
            e.HasOne(x => x.Specimen).WithMany()
                .HasForeignKey(x => x.SpecimenId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ManifestId, x.Status });

            e.HasQueryFilter(x => x.LabId == CurrentLabId);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantOnWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        EnforceTenantOnWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    /// <summary>
    /// Second line of defence. Query filters stop a lab *reading* another lab's rows, which
    /// means it normally cannot get its hands on one to modify. This guard covers the paths
    /// filters do not: a hand-built entity with an explicit Id, a stale tracked instance, or
    /// an insert that simply forgot to set LabId.
    /// </summary>
    private void EnforceTenantOnWrites()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    StampOrVerify(entry);
                    break;

                case EntityState.Modified:
                case EntityState.Deleted:
                    Verify(entry.Entity.LabId);
                    // Reassigning a row to another lab is never a legal edit.
                    var original = entry.OriginalValues[nameof(ITenantOwned.LabId)];
                    if (original is int originalLabId && originalLabId != entry.Entity.LabId)
                        throw new CrossTenantWriteException(originalLabId, entry.Entity.LabId);
                    break;
            }
        }
    }

    private void StampOrVerify(EntityEntry<ITenantOwned> entry)
    {
        if (entry.Entity.LabId == 0)
        {
            entry.Entity.LabId = _tenant.RequireLabId();
            return;
        }

        Verify(entry.Entity.LabId);
    }

    private void Verify(int labId)
    {
        // No tenant means we are seeding or running design-time tooling, where rows carry
        // their LabId explicitly. The HTTP pipeline can never reach this branch: every
        // /api request passes through TenantMiddleware, which rejects an unresolved tenant.
        if (!_tenant.HasTenant) return;

        if (labId != _tenant.LabId)
            throw new CrossTenantWriteException(_tenant.LabId!.Value, labId);
    }
}

public sealed class CrossTenantWriteException : Exception
{
    public CrossTenantWriteException(int expectedLabId, int actualLabId)
        : base($"Refused a write scoped to lab {expectedLabId} that targeted lab {actualLabId}.")
    {
        ExpectedLabId = expectedLabId;
        ActualLabId = actualLabId;
    }

    public int ExpectedLabId { get; }
    public int ActualLabId { get; }
}
