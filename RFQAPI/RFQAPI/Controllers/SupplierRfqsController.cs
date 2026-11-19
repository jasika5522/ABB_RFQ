using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Rfqs;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/supplier/rfqs")]
[Authorize(Roles = UserRoles.Supplier)]
public class SupplierRfqsController : ControllerBase
{
    private readonly ISupplierRfqService _supplierRfqService;

    public SupplierRfqsController(ISupplierRfqService supplierRfqService)
    {
        _supplierRfqService = supplierRfqService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedSupplierAvailableRfqsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedSupplierAvailableRfqsResponse>> GetAvailable(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? rfqNumber = null,
        [FromQuery] string? title = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierRfqService.GetAvailableAsync(
            page, pageSize, rfqNumber, title, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{rfqId:long}")]
    [ProducesResponseType(typeof(SupplierAvailableRfqDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierAvailableRfqDetailResponse>> GetAvailableById(
        long rfqId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var detail = await _supplierRfqService.GetAvailableDetailAsync(rfqId, userId, cancellationToken);
        return Ok(detail);
    }
}