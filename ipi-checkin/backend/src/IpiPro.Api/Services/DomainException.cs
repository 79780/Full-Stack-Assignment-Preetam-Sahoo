namespace IpiPro.Api.Services;

/// <summary>
/// A rule the caller broke, as opposed to a bug. Carries a stable machine-readable code so
/// the front-end can branch on the failure without string-matching an English sentence.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string code, string message, int statusCode = StatusCodes.Status409Conflict)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }

    public static DomainException NotFound(string what) =>
        new("not_found", $"{what} was not found.", StatusCodes.Status404NotFound);
}
