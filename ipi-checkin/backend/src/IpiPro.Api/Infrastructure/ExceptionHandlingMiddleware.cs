using System.Text.Json;
using IpiPro.Api.Data;
using IpiPro.Api.Services;

namespace IpiPro.Api.Infrastructure;

/// <summary>
/// One place that turns exceptions into RFC 7807 problem+json. Every failure the client can
/// act on arrives in the same shape, with a stable "code" it can switch on.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext http)
    {
        try
        {
            await _next(http);
        }
        catch (DomainException ex)
        {
            // Expected: the caller broke a rule. Not an error worth a stack trace.
            _log.LogInformation("Rejected {Method} {Path}: {Code}",
                http.Request.Method, http.Request.Path, ex.Code);

            await Write(http, ex.StatusCode, ex.Code, "Request rejected.", ex.Message);
        }
        catch (CrossTenantWriteException ex)
        {
            // Unexpected: a code path tried to write across the tenant boundary. That is a
            // bug or an attack, and either way it is the loudest log line in the file.
            _log.LogError(ex, "Cross-tenant write blocked on {Method} {Path}",
                http.Request.Method, http.Request.Path);

            await Write(http, StatusCodes.Status404NotFound, "not_found",
                "Request rejected.", "The requested resource was not found.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled error on {Method} {Path}",
                http.Request.Method, http.Request.Path);

            await Write(http, StatusCodes.Status500InternalServerError, "internal_error",
                "Something went wrong.", "The request could not be completed.");
        }
    }

    private static async Task Write(HttpContext http, int status, string code, string title, string detail)
    {
        if (http.Response.HasStarted) return;

        http.Response.Clear();
        http.Response.StatusCode = status;
        http.Response.ContentType = "application/problem+json";

        await http.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = "about:blank",
            title,
            status,
            code,
            detail,
            traceId = http.TraceIdentifier
        }, Json));
    }
}
