using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Awards;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
public class RfqAwardsController : ControllerBase
{
    private readonly IAwardService _awardService;

    public RfqAwardsController(IAwardService awardService)
    {
        _awardService = awardService;
    }

    [HttpPost("/api/rfqs/{rfqId:long}/rounds/{roundId:long}/award")]
    [Authorize(Roles = UserRoles.Approver)]
    [ProducesResponseType(typeof(AwardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AwardResponse>> Create(
        long rfqId,
        long roundId,
        [FromBody] CreateAwardRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var award = await _awardService.CreateAsync(
            rfqId, roundId, request, userId, GetClientIpAddress(), cancellationToken);

        return CreatedAtAction(
            nameof(GetByRfq),
            new { rfqId },
            award);
    }

    [HttpGet("/api/rfqs/{rfqId:long}/award")]
    [Authorize(Roles = $"{UserRoles.ProcurementOfficer},{UserRoles.Approver}")]
    [ProducesResponseType(typeof(AwardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AwardResponse>> GetByRfq(
        long rfqId,
        CancellationToken cancellationToken)
    {
        var award = await _awardService.GetByRfqIdAsync(rfqId, cancellationToken);
        return Ok(award);
    }

    private string? GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        return ipAddress is "::1" ? IPAddress.Loopback.ToString() : ipAddress;
    }
}