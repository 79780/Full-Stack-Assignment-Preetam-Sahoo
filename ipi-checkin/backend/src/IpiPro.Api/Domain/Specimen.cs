namespace IpiPro.Api.Domain;

public class Specimen : ITenantOwned
{
    public int Id { get; set; }
    public int LabId { get; set; }

    public int ManifestId { get; set; }
    public Manifest? Manifest { get; set; }

    /// <summary>Accession / barcode value printed on the bottle.</summary>
    public string Code { get; set; } = string.Empty;

    public string Patient { get; set; } = string.Empty;

    public SpecimenStatus Status { get; set; } = SpecimenStatus.Pending;

    public DateTime? ReceivedAt { get; set; }
}
