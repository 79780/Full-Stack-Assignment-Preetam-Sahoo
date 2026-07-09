using IpiPro.Api.Data;
using IpiPro.Api.Domain;
using IpiPro.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IpiPro.Api.Tests;

/// <summary>
/// These tests are the reason the tenant key is enforced in the DbContext rather than in each
/// query: they pin the boundary itself, so they keep holding as endpoints are added.
/// </summary>
public class TenantIsolationTests
{
    [Fact]
    public async Task A_lab_only_sees_its_own_manifests()
    {
        await using var harness = await TestHarness.CreateAsync();

        var (_, northgate) = harness.AsLab(TestHarness.Northgate);
        var (_, ridgeview) = harness.AsLab(TestHarness.Ridgeview);

        var mine = await northgate.ListManifestsAsync();
        var theirs = await ridgeview.ListManifestsAsync();

        Assert.Equal(3, mine.Count);
        Assert.Single(theirs);
        Assert.Equal("Ridgeview Outpatients", theirs[0].ClinicName);
    }

    [Fact]
    public async Task Another_labs_manifest_is_not_found_rather_than_forbidden()
    {
        await using var harness = await TestHarness.CreateAsync();

        var otherId = await OtherLabManifestId(harness);
        var (_, northgate) = harness.AsLab(TestHarness.Northgate);

        var ex = await Assert.ThrowsAsync<DomainException>(() => northgate.GetManifestAsync(otherId));

        // 404, not 403: a 403 would confirm the id exists somewhere.
        Assert.Equal("not_found", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task A_lab_cannot_receive_a_specimen_belonging_to_another_lab()
    {
        await using var harness = await TestHarness.CreateAsync();

        await using var system = harness.AsSystem();
        var other = await system.Manifests.IgnoreQueryFilters()
            .Include(m => m.Specimens)
            .FirstAsync(m => m.LabId == TestHarness.Ridgeview);

        var specimenId = other.Specimens.First(s => s.Status == SpecimenStatus.Pending).Id;

        var (_, northgate) = harness.AsLab(TestHarness.Northgate);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => northgate.ReceiveSpecimenAsync(other.Id, specimenId));

        Assert.Equal("not_found", ex.Code);

        // And nothing moved.
        await using var check = harness.AsSystem();
        var specimen = await check.Specimens.IgnoreQueryFilters().FirstAsync(s => s.Id == specimenId);
        Assert.Equal(SpecimenStatus.Pending, specimen.Status);
    }

    [Fact]
    public async Task Writing_a_row_stamped_with_another_lab_is_refused_at_SaveChanges()
    {
        await using var harness = await TestHarness.CreateAsync();

        var (db, _) = harness.AsLab(TestHarness.Northgate);

        db.Manifests.Add(new Manifest
        {
            LabId = TestHarness.Ridgeview, // deliberately wrong
            Code = "MF-9999",
            ClinicName = "Nowhere",
            SentAt = DateTime.UtcNow
        });

        var ex = await Assert.ThrowsAsync<CrossTenantWriteException>(() => db.SaveChangesAsync());

        Assert.Equal(TestHarness.Northgate, ex.ExpectedLabId);
        Assert.Equal(TestHarness.Ridgeview, ex.ActualLabId);
    }

    [Fact]
    public async Task An_insert_that_forgets_its_LabId_is_stamped_with_the_current_lab()
    {
        await using var harness = await TestHarness.CreateAsync();

        var (db, _) = harness.AsLab(TestHarness.Northgate);

        var manifest = new Manifest
        {
            Code = "MF-3000",
            ClinicName = "Elm Row Practice",
            SentAt = DateTime.UtcNow
        };
        db.Manifests.Add(manifest);
        await db.SaveChangesAsync();

        Assert.Equal(TestHarness.Northgate, manifest.LabId);
    }

    [Fact]
    public async Task Manifest_codes_are_unique_per_lab_not_globally()
    {
        await using var harness = await TestHarness.CreateAsync();

        // The seed gives both labs an MF-2481, and the migration applied cleanly, so the
        // unique index really is scoped to (LabId, Code).
        await using var system = harness.AsSystem();
        var count = await system.Manifests.IgnoreQueryFilters().CountAsync(m => m.Code == "MF-2481");

        Assert.Equal(2, count);
    }

    private static async Task<int> OtherLabManifestId(TestHarness harness)
    {
        await using var system = harness.AsSystem();
        return await system.Manifests.IgnoreQueryFilters()
            .Where(m => m.LabId == TestHarness.Ridgeview)
            .Select(m => m.Id)
            .FirstAsync();
    }
}
