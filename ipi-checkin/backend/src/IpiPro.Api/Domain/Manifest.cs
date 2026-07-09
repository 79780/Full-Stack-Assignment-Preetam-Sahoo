namespace IpiPro.Api.Domain;

public class Manifest : ITenantOwned
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public Lab? Lab { get; set; }

    /// <summary>Human-facing manifest number, e.g. MF-2481. Unique within a lab.</summary>
    public string Code { get; set; } = string.Empty;

    public string ClinicName { get; set; } = string.Empty;

    public ManifestStatus Status { get; set; } = ManifestStatus.Open;

    public DateTime SentAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<Specimen> Specimens { get; set; } = new List<Specimen>();
    public ICollection<Discrepancy> Discrepancies { get; set; } = new List<Discrepancy>();

    /// <summary>
    /// Reconciled means every listed bottle has been actioned — received at the desk or
    /// flagged as missing. A Pending bottle means the technician is still looking.
    /// </summary>
    public bool IsReconciled() => Specimens.All(s => s.Status != SpecimenStatus.Pending);

    public bool IsClosed() => Status != ManifestStatus.Open;
}
