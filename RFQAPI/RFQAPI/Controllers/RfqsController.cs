using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Rfqs;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/rfqs")]
[Authorize(Roles = $"{UserRoles.ProcurementOfficer},{UserRoles.Approver}")]
public class RfqsController : ControllerBase
{
    private readonly IRfqService _rfqService;

    public RfqsController(IRfqService rfqService)
    {
        _rfqService = rfqService;
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.ProcurementOfficer)]
    [ProducesResponseType(typeof(RfqDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RfqDetailResponse>> Create(
        [FromBody] CreateRfqRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var ipAddress = GetClientIpAddress();
        var rfq = await _rfqService.CreateAsync(request, userId, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { rfqId = rfq.RfqId }, rfq);
    }

    [HttpPut("{rfqId:long}")]
    [Authorize(Roles = UserRoles.ProcurementOfficer)]
    [ProducesResponseType(typeof(RfqDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqDetailResponse>> Update(
        long rfqId,
        [FromBody] UpdateRfqRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var ipAddress = GetClientIpAddress();
        var rfq = await _rfqService.UpdateDraftAsync(rfqId, request, userId, ipAddress, cancellationToken);
        return Ok(rfq);
    }

    [HttpGet("{rfqId:long}")]
    [ProducesResponseType(typeof(RfqDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RfqDetailResponse>> GetById(
        long rfqId,
        CancellationToken cancellationToken)
    {
        var rfq = await _rfqService.GetByIdAsync(rfqId, cancellationToken);
        return Ok(rfq);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedRfqsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedRfqsResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? rfqNumber = null,
        [FromQuery] string? title = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _rfqService.GetAllAsync(page, pageSize, status, rfqNumber, title, cancellationToken);
        return Ok(result);
    }

    private string? GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        return ipAddress is "::1" ? IPAddress.Loopback.ToString() : ipAddress;
    }
}