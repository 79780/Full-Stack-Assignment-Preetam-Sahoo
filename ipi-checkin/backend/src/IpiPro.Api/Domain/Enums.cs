namespace IpiPro.Api.Domain;

public enum ManifestStatus
{
    Open,
    Closed,
    ClosedWithDiscrepancy
}

public enum SpecimenStatus
{
    Pending,
    Received,
    Flagged
}

public enum DiscrepancyType
{
    Missing,
    OffManifest
}

public enum DiscrepancyStatus
{
    Open,
    Resolved
}
