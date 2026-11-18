using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Suppliers;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet("me")]
    [Authorize(Roles = UserRoles.Supplier)]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var profile = await _supplierService.GetMyProfileAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("me")]
    [Authorize(Roles = UserRoles.Supplier)]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierResponse>> UpdateMyProfile(
        [FromBody] UpdateSupplierProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var profile = await _supplierService.UpdateMyProfileAsync(userId, request, cancellationToken);
        return Ok(profile);
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.ProcurementOfficer},{UserRoles.Approver}")]
    [ProducesResponseType(typeof(PagedSuppliersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedSuppliersResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? approvalStatus = null,
        [FromQuery] string? companyName = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _supplierService.GetAllAsync(
            page, pageSize, approvalStatus, companyName, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{supplierId:long}")]
    [Authorize(Roles = $"{UserRoles.ProcurementOfficer},{UserRoles.Approver}")]
    [ProducesResponseType(typeof(SupplierDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDetailResponse>> GetById(
        long supplierId,
        CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(supplierId, cancellationToken);
        return Ok(supplier);
    }

    [HttpPatch("{supplierId:long}/status")]
    [Authorize(Roles = UserRoles.ProcurementOfficer)]
    [ProducesResponseType(typeof(SupplierDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDetailResponse>> UpdateStatus(
        long supplierId,
        [FromBody] UpdateSupplierStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actingUserId = User.GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (ipAddress is "::1")
        {
            ipAddress = IPAddress.Loopback.ToString();
        }

        var supplier = await _supplierService.UpdateApprovalStatusAsync(
            supplierId,
            actingUserId,
            request.ApprovalStatus,
            ipAddress,
            cancellationToken);

        return Ok(supplier);
    }
}