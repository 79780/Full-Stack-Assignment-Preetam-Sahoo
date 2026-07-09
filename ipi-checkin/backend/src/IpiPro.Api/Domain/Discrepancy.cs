namespace IpiPro.Api.Domain;

public class Discrepancy : ITenantOwned
{
    public int Id { get; set; }
    public int LabId { get; set; }

    public int ManifestId { get; set; }
    public Manifest? Manifest { get; set; }

    /// <summary>Null only for an off-manifest bottle, which has no listed specimen row.</summary>
    public int? SpecimenId { get; set; }
    public Specimen? Specimen { get; set; }

    public DiscrepancyType Type { get; set; }
    public DiscrepancyStatus Status { get; set; } = DiscrepancyStatus.Open;

    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
