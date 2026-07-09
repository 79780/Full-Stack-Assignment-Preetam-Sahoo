using IpiPro.Api.Data;
using IpiPro.Api.Domain;
using IpiPro.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IpiPro.Api.Tests;

public class ReconciliationTests
{
    /// <summary>MF-2481: 2 received, 1 flagged, 3 pending, 1 open discrepancy.</summary>
    private static async Task<(TestHarness Harness, AppDbContext Db, CheckInService Svc, Manifest M)>
        OpenManifestAsync()
    {
        var harness = await TestHarness.CreateAsync();
        var (db, svc) = harness.AsLab(TestHarness.Northgate);

        var manifest = await db.Manifests
            .Include(m => m.Specimens)
            .FirstAsync(m => m.Code == "MF-2481");

        return (harness, db, svc, manifest);
    }

    [Fact]
    public async Task Receiving_the_same_specimen_twice_does_not_move_the_counts()
    {
        var (harness, _, svc, m) = await OpenManifestAsync();
        await using var _h = harness;

        var pending = m.Specimens.First(s => s.Status == SpecimenStatus.Pending);

        var first = await svc.ReceiveSpecimenAsync(m.Id, pending.Id);
        var second = await svc.ReceiveSpecimenAsync(m.Id, pending.Id);

        Assert.Equal(3, first.Counts.Received);
        Assert.Equal(3, second.Counts.Received);
        Assert.Equal(2, second.Counts.Pending);
        Assert.Equal(first.Counts, second.Counts);
    }

    [Fact]
    public async Task Flagging_the_same_specimen_twice_raises_exactly_one_discrepancy()
    {
        var (harness, db, svc, m) = await OpenManifestAsync();
        await using var _h = harness;

        var pending = m.Specimens.First(s => s.Status == SpecimenStatus.Pending);

        await svc.FlagSpecimenAsync(m.Id, pending.Id);
        var result = await svc.FlagSpecimenAsync(m.Id, pending.Id);

        var open = await db.Discrepancies
            .CountAsync(d => d.SpecimenId == pending.Id && d.Status == DiscrepancyStatus.Open);

        Assert.Equal(1, open);
        Assert.Equal(SpecimenStatus.Flagged,
            result.Specimens.Single(s => s.Id == pending.Id).Status);
    }

    [Fact]
    public async Task Receiving_a_flagged_specimen_resolves_the_discrepancy_it_raised()
    {
        var (harness, db, svc, m) = await OpenManifestAsync();
        await using var _h = harness;

        var flagged = m.Specimens.First(s => s.Status == SpecimenStatus.Flagged);

        var result = await svc.ReceiveSpecimenAsync(m.Id, flagged.Id);

        Assert.Equal(0, result.Counts.Flagged);
        Assert.Equal(0, result.Counts.OpenDiscrepancies);

        var discrepancy = await db.Discrepancies.SingleAsync(d => d.SpecimenId == flagged.Id);
        Assert.Equal(DiscrepancyStatus.Resolved, discrepancy.Status);
        Assert.NotNull(discrepancy.ResolvedAt);
    }

    [Fact]
    public async Task Flagging_an_already_received_specimen_is_rejected()
    {
        var (harness, _, svc, m) = await OpenManifestAsync();
        await using var _h = harness;

        var received = m.Specimens.First(s => s.Status == SpecimenStatus.Received);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => svc.FlagSpecimenAsync(m.Id, received.Id));

        Assert.Equal("specimen_already_received", ex.Code);
    }

    [Fact]
    public async Task Closing_is_rejected_while_any_specimen_is_pending()
    {
        var (harness, _, svc, m) = await OpenManifestAsync();
        await using var _h = harness;

        var ex = await Assert.ThrowsAsync<DomainException>(() => svc.CloseManifestAsync(m.Id));

        Assert.Equal("manifest_not_reconciled", ex.Code);
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Closing_a_fully_received_manifest_marks_it_Closed()
    {
        var (harness, _, svc, m) = await OpenManifestAsync();
        await using var _h = harness;

        foreach (var s in m.Specimens)
            await svc.ReceiveSpecimenAsync(m.Id, s.Id);

        var result = await svc.CloseManifestAsync(m.Id);

        Assert.Equal(ManifestStatus.Closed, result.Status);
        Assert.Equal(0, result.Counts.OpenDiscrepancies);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task Closing_a_reconciled_manifest_that_still_has_a_missing_bottle_records_the_discrepancy()
    {
        var (harness, _, svc, m) = await OpenManifestAsync();
        await using var _h = harness;

        // Every pending bottle turns up; the one flagged as missing never does.
        foreach (var s in m.Specimens.Where(s => s.Status == SpecimenStatus.Pending))
            await svc.ReceiveSpecimenAsync(m.Id, s.Id);

        var result = await svc.CloseManifestAsync(m.Id);

        Assert.Equal(ManifestStatus.ClosedWithDiscrepancy, result.Status);
        Assert.Equal(1, result.Counts.OpenDiscrepancies);
    }

    [Fact]
    public async Task A_closed_manifest_accepts_no_further_check_in_actions()
    {
        var harness = await TestHarness.CreateAsync();
        await using var _h = harness;

        var (db, svc) = harness.AsLab(TestHarness.Northgate);
        var closed = await db.Manifests.Include(m => m.Specimens).FirstAsync(m => m.Code == "MF-2475");
        var specimen = closed.Specimens.First();

        var receive = await Assert.ThrowsAsync<DomainException>(
            () => svc.ReceiveSpecimenAsync(closed.Id, specimen.Id));
        Assert.Equal("manifest_closed", receive.Code);

        var close = await Assert.ThrowsAsync<DomainException>(() => svc.CloseManifestAsync(closed.Id));
        Assert.Equal("manifest_already_closed", close.Code);
    }
}
