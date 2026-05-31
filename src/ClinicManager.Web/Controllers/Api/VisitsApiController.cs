using ClinicManager.Web.DTOs;
using ClinicManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Web.Controllers.Api;

/// <summary>
/// API wizyt (m.in. endpoint pod testy wydajnościowe NBomber).
/// </summary>
[ApiController]
[Route("api/visits")]
[Produces("application/json")]
public class VisitsApiController : ControllerBase
{
    private readonly IVisitService _visitService;

    public VisitsApiController(IVisitService visitService)
    {
        _visitService = visitService;
    }

    /// <summary>
    /// Zwraca aktywne wizyty (Zaplanowana, Potwierdzona, W trakcie) z danymi pacjenta i lekarza.
    /// </summary>
    /// <remarks>
    /// Endpoint dedykowany do testów obciążeniowych (NBomber). Wykonuje zapytanie z JOIN-ami do bazy.
    /// </remarks>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ActiveVisitApiDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActiveVisitApiDto>>> GetActive()
    {
        var visits = await _visitService.GetActiveVisitsAsync();
        return Ok(visits);
    }
}
