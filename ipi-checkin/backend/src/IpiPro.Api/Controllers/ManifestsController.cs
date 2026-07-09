using IpiPro.Api.Contracts;
using IpiPro.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IpiPro.Api.Controllers;

[ApiController]
[Route("api/manifests")]
[Produces("application/json")]
public class ManifestsController : ControllerBase
{
    private readonly CheckInService _checkIn;

    public ManifestsController(CheckInService checkIn) => _checkIn = checkIn;

    /// <summary>Worklist for the current lab.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManifestSummaryDto>>> List(CancellationToken ct)
        => Ok(await _checkIn.ListManifestsAsync(ct));

    /// <summary>Manifest detail. 404 when the manifest belongs to another lab.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManifestDetailDto>> Get(int id, CancellationToken ct)
        => Ok(await _checkIn.GetManifestAsync(id, ct));

    /// <summary>Mark a bottle as physically received. Safe to call more than once.</summary>
    [HttpPost("{id:int}/specimens/{specimenId:int}/receive")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManifestDetailDto>> Receive(int id, int specimenId, CancellationToken ct)
        => Ok(await _checkIn.ReceiveSpecimenAsync(id, specimenId, ct));

    /// <summary>Flag a listed bottle as missing. Raises one open discrepancy.</summary>
    [HttpPost("{id:int}/specimens/{specimenId:int}/flag")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManifestDetailDto>> Flag(int id, int specimenId, CancellationToken ct)
        => Ok(await _checkIn.FlagSpecimenAsync(id, specimenId, ct));

    /// <summary>Close the manifest. Refused while any bottle is still pending.</summary>
    [HttpPost("{id:int}/close")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManifestDetailDto>> Close(int id, CancellationToken ct)
        => Ok(await _checkIn.CloseManifestAsync(id, ct));
}
