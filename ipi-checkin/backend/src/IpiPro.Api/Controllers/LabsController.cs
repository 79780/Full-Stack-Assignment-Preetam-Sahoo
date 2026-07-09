using IpiPro.Api.Data;
using IpiPro.Api.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IpiPro.Api.Controllers;

[ApiController]
[Route("api/labs")]
public class LabsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public LabsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>
    /// Who am I? The UI shows this in the header so it is always obvious which lab's data
    /// is on screen. Note it reads the id from ITenantContext, not from the request.
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<object>> Current(CancellationToken ct)
    {
        var labId = _tenant.RequireLabId();
        var lab = await _db.Labs.AsNoTracking().FirstAsync(l => l.Id == labId, ct);
        return Ok(new { lab.Id, lab.Name });
    }
}
