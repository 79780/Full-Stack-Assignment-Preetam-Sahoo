namespace IpiPro.Api.Domain;

/// <summary>The isolation boundary. Deliberately not ITenantOwned: it *is* the tenant.</summary>
public class Lab
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Manifest> Manifests { get; set; } = new List<Manifest>();
}
