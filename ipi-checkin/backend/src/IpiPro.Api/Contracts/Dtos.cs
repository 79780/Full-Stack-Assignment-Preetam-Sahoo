using IpiPro.Api.Domain;

namespace IpiPro.Api.Contracts;

/// <summary>Left-hand worklist row.</summary>
public record ManifestSummaryDto(
    int Id,
    string Code,
    string ClinicName,
    ManifestStatus Status,
    DateTime SentAt,
    ManifestCountsDto Counts);

public record ManifestDetailDto(
    int Id,
    string Code,
    string ClinicName,
    ManifestStatus Status,
    DateTime SentAt,
    DateTime? ClosedAt,
    ManifestCountsDto Counts,
    IReadOnlyList<SpecimenDto> Specimens);

/// <summary>
/// The counts the technician actually reads off the screen. Computed server-side so the
/// UI never has to re-derive "ready to close" and risk disagreeing with the close endpoint.
/// </summary>
public record ManifestCountsDto(
    int Total,
    int Received,
    int Flagged,
    int Pending,
    int OpenDiscrepancies,
    bool IsReconciled,
    bool CanClose);

public record SpecimenDto(
    int Id,
    string Code,
    string Patient,
    SpecimenStatus Status,
    DateTime? ReceivedAt);

public static class DtoMapper
{
    public static ManifestCountsDto ToCounts(Manifest m, int openDiscrepancies)
    {
        var received = m.Specimens.Count(s => s.Status == SpecimenStatus.Received);
        var flagged = m.Specimens.Count(s => s.Status == SpecimenStatus.Flagged);
        var pending = m.Specimens.Count(s => s.Status == SpecimenStatus.Pending);
        var reconciled = pending == 0;

        return new ManifestCountsDto(
            Total: m.Specimens.Count,
            Received: received,
            Flagged: flagged,
            Pending: pending,
            OpenDiscrepancies: openDiscrepancies,
            IsReconciled: reconciled,
            CanClose: reconciled && !m.IsClosed());
    }

    public static ManifestSummaryDto ToSummary(Manifest m, int openDiscrepancies) =>
        new(m.Id, m.Code, m.ClinicName, m.Status, m.SentAt, ToCounts(m, openDiscrepancies));

    public static ManifestDetailDto ToDetail(Manifest m, int openDiscrepancies) =>
        new(m.Id, m.Code, m.ClinicName, m.Status, m.SentAt, m.ClosedAt,
            ToCounts(m, openDiscrepancies),
            m.Specimens
                .OrderBy(s => s.Code, StringComparer.Ordinal)
                .Select(s => new SpecimenDto(s.Id, s.Code, s.Patient, s.Status, s.ReceivedAt))
                .ToList());
}
