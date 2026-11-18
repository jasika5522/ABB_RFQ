using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Awards;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/awards")]
[Authorize(Roles = $"{UserRoles.ProcurementOfficer},{UserRoles.Approver}")]
public class AwardsController : ControllerBase
{
    private readonly IAwardService _awardService;

    public AwardsController(IAwardService awardService)
    {
        _awardService = awardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedAwardsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedAwardsResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? rfqNumber = null,
        [FromQuery] string? companyName = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _awardService.GetAllAsync(page, pageSize, rfqNumber, companyName, cancellationToken);
        return Ok(result);
    }
}