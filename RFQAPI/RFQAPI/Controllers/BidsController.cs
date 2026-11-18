using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Bids;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/bids")]
[Authorize(Roles = UserRoles.Supplier)]
public class BidsController : ControllerBase
{
    private readonly IBidService _bidService;

    public BidsController(IBidService bidService)
    {
        _bidService = bidService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(PagedBidsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedBidsResponse>> GetMyBids(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] long? rfqId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var result = await _bidService.GetMyBidsAsync(userId, page, pageSize, rfqId, status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{bidId:long}")]
    [ProducesResponseType(typeof(BidDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BidDetailResponse>> GetById(
        long bidId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var bid = await _bidService.GetMyBidDetailAsync(bidId, userId, cancellationToken);
        return Ok(bid);
    }

    [HttpPatch("{bidId:long}/withdraw")]
    [ProducesResponseType(typeof(BidResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BidResponse>> Withdraw(
        long bidId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var bid = await _bidService.WithdrawAsync(bidId, userId, GetClientIpAddress(), cancellationToken);
        return Ok(bid);
    }

    private string? GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        return ipAddress is "::1" ? IPAddress.Loopback.ToString() : ipAddress;
    }
}