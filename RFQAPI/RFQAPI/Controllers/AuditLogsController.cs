using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.AuditLogs;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = $"{UserRoles.ProcurementOfficer},{UserRoles.Approver}")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedAuditLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedAuditLogsResponse>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] long? entityId = null,
        [FromQuery] long? userId = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new AuditLogQueryParameters
        {
            Page = page,
            PageSize = pageSize,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _auditLogService.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("entity/{entityType}/{entityId:long}")]
    [ProducesResponseType(typeof(PagedAuditLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedAuditLogsResponse>> GetByEntity(
        string entityType,
        long entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditLogService.GetByEntityAsync(
            entityType, entityId, page, pageSize, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{auditId:long}")]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogResponse>> GetById(
        long auditId,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetByIdAsync(auditId, cancellationToken);
        return Ok(result);
    }
}