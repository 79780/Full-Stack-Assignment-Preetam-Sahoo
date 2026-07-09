namespace IpiPro.Api.Tenancy;

/// <summary>
/// The current lab, resolved once per request. Everything downstream — the DbContext
/// query filter, the SaveChanges guard — reads the tenant from here and nowhere else.
/// </summary>
public interface ITenantContext
{
    /// <summary>Null during seeding and in design-time tooling; never null on an HTTP request.</summary>
    int? LabId { get; }

    bool HasTenant { get; }

    /// <summary>Throws if no tenant is resolved. Use wherever a lab is genuinely required.</summary>
    int RequireLabId();
}
