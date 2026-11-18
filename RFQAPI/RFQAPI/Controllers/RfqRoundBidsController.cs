using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Bids;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/rfqs/{rfqId:long}/rounds/{roundId:long}/bids")]
[Authorize(Roles = UserRoles.Supplier)]
public class RfqRoundBidsController : ControllerBase
{
    private readonly IBidService _bidService;

    public RfqRoundBidsController(IBidService bidService)
    {
        _bidService = bidService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BidResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BidResponse>> Submit(
        long rfqId,
        long roundId,
        [FromBody] SubmitBidRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var bid = await _bidService.SubmitAsync(
            rfqId, roundId, userId, request, GetClientIpAddress(), cancellationToken);

        return CreatedAtAction(
            nameof(BidsController.GetById),
            "Bids",
            new { bidId = bid.BidId },
            bid);
    }

    private string? GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        return ipAddress is "::1" ? IPAddress.Loopback.ToString() : ipAddress;
    }
}