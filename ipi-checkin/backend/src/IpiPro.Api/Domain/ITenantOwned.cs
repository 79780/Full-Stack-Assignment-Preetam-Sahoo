namespace IpiPro.Api.Domain;

/// <summary>
/// Every row a lab can own carries its own LabId. Denormalising the tenant key onto each
/// table (rather than walking a FK chain back to Manifest) is what lets one EF global
/// query filter cover the whole model. See AppDbContext.
/// </summary>
public interface ITenantOwned
{
    int LabId { get; set; }
}
