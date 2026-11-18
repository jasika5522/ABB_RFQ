using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Rounds;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/rfqs/{rfqId:long}/rounds")]
[Authorize(Roles = $"{UserRoles.ProcurementOfficer},{UserRoles.Approver}")]
public class RfqRoundsController : ControllerBase
{
    private readonly IRfqRoundService _rfqRoundService;

    public RfqRoundsController(IRfqRoundService rfqRoundService)
    {
        _rfqRoundService = rfqRoundService;
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.ProcurementOfficer)]
    [ProducesResponseType(typeof(RfqRoundResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RfqRoundResponse>> Create(
        long rfqId,
        [FromBody] CreateRfqRoundRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var round = await _rfqRoundService.CreateAsync(
            rfqId, request, userId, GetClientIpAddress(), cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { rfqId, roundId = round.RoundId },
            round);
    }

    [HttpPatch("{roundId:long}/publish")]
    [Authorize(Roles = UserRoles.ProcurementOfficer)]
    [ProducesResponseType(typeof(RfqRoundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RfqRoundResponse>> Publish(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var round = await _rfqRoundService.PublishAsync(
            rfqId, roundId, userId, GetClientIpAddress(), cancellationToken);

        return Ok(round);
    }

    [HttpPatch("{roundId:long}/close")]
    [Authorize(Roles = UserRoles.ProcurementOfficer)]
    [ProducesResponseType(typeof(RfqRoundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqRoundResponse>> Close(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var round = await _rfqRoundService.CloseAsync(
            rfqId, roundId, userId, GetClientIpAddress(), cancellationToken);

        return Ok(round);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RfqRoundResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RfqRoundResponse>>> GetAll(
        long rfqId,
        CancellationToken cancellationToken)
    {
        var rounds = await _rfqRoundService.GetByRfqIdAsync(rfqId, cancellationToken);
        return Ok(rounds);
    }

    [HttpGet("{roundId:long}")]
    [ProducesResponseType(typeof(RfqRoundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqRoundResponse>> GetById(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        var round = await _rfqRoundService.GetByIdAsync(rfqId, roundId, cancellationToken);
        return Ok(round);
    }

    private string? GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        return ipAddress is "::1" ? IPAddress.Loopback.ToString() : ipAddress;
    }
}