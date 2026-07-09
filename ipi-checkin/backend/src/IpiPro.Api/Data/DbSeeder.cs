using IpiPro.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IpiPro.Api.Data;

/// <summary>
/// Synthetic seed data only — no real patient information, ever. Two labs, so tenant
/// isolation is something you can see by flipping a header rather than something you
/// have to take on trust.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: seeding runs without a tenant, so the filters would otherwise
        // hide every row and we would re-seed on each boot.
        if (await db.Manifests.IgnoreQueryFilters().AnyAsync(ct)) return;

        var sentAt = new DateTime(2026, 7, 6, 8, 15, 0, DateTimeKind.Utc);

        var northgate = new Lab { Name = "Northgate Pathology" };
        var ridgeview = new Lab { Name = "Ridgeview Diagnostics" };
        db.Labs.AddRange(northgate, ridgeview);
        await db.SaveChangesAsync(ct);

        // --- Lab 1: the manifest the design reference is showing --------------------
        var busy = new Manifest
        {
            LabId = northgate.Id,
            Code = "MF-2481",
            ClinicName = "Vale Street Clinic",
            Status = ManifestStatus.Open,
            SentAt = sentAt,
            Specimens =
            {
                Spec(northgate.Id, "SPC-10041", "Harper Quinn", SpecimenStatus.Received, sentAt),
                Spec(northgate.Id, "SPC-10042", "Marcus Ellery", SpecimenStatus.Received, sentAt),
                Spec(northgate.Id, "SPC-10043", "Nadia Osei", SpecimenStatus.Flagged),
                Spec(northgate.Id, "SPC-10044", "Theo Whitlock", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10045", "Imogen Bassey", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10046", "Rafael Duarte", SpecimenStatus.Pending)
            }
        };

        var fresh = new Manifest
        {
            LabId = northgate.Id,
            Code = "MF-2482",
            ClinicName = "Kingsmead Surgery",
            Status = ManifestStatus.Open,
            SentAt = sentAt.AddHours(3),
            Specimens =
            {
                Spec(northgate.Id, "SPC-10051", "Priya Raman", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10052", "Callum Reed", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10053", "Ana Ferreira", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10054", "Dominic Shaw", SpecimenStatus.Pending)
            }
        };

        var settled = new Manifest
        {
            LabId = northgate.Id,
            Code = "MF-2475",
            ClinicName = "Vale Street Clinic",
            Status = ManifestStatus.Closed,
            SentAt = sentAt.AddDays(-2),
            ClosedAt = sentAt.AddDays(-2).AddHours(5),
            Specimens =
            {
                Spec(northgate.Id, "SPC-09980", "Elena Kovac", SpecimenStatus.Received, sentAt.AddDays(-2)),
                Spec(northgate.Id, "SPC-09981", "Owen Brackley", SpecimenStatus.Received, sentAt.AddDays(-2))
            }
        };

        // --- Lab 2: exists purely so that "Lab A cannot see this" is demonstrable ----
        var otherLab = new Manifest
        {
            LabId = ridgeview.Id,
            Code = "MF-2481", // same code as Northgate's: codes are unique per lab, not globally
            ClinicName = "Ridgeview Outpatients",
            Status = ManifestStatus.Open,
            SentAt = sentAt.AddHours(1),
            Specimens =
            {
                Spec(ridgeview.Id, "SPC-77010", "Joan Mbeki", SpecimenStatus.Received, sentAt),
                Spec(ridgeview.Id, "SPC-77011", "Peter Lindqvist", SpecimenStatus.Pending),
                Spec(ridgeview.Id, "SPC-77012", "Sasha Delaney", SpecimenStatus.Pending)
            }
        };

        db.Manifests.AddRange(busy, fresh, settled, otherLab);
        await db.SaveChangesAsync(ct);

        // The flagged bottle on MF-2481 carries the open discrepancy it would have raised.
        db.Discrepancies.Add(new Discrepancy
        {
            LabId = northgate.Id,
            ManifestId = busy.Id,
            SpecimenId = busy.Specimens.First(s => s.Code == "SPC-10043").Id,
            Type = DiscrepancyType.Missing,
            Status = DiscrepancyStatus.Open,
            CreatedAt = sentAt.AddHours(1)
        });

        await db.SaveChangesAsync(ct);
    }

    private static Specimen Spec(
        int labId, string code, string patient, SpecimenStatus status, DateTime? receivedAt = null) =>
        new()
        {
            LabId = labId,
            Code = code,
            Patient = patient,
            Status = status,
            ReceivedAt = status == SpecimenStatus.Received ? receivedAt : null
        };
}
