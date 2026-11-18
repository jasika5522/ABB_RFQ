using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Approval;
using RFQAPI.Models.DTOs.Evaluation;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/rfqs/{rfqId:long}/rounds/{roundId:long}")]
[Authorize(Roles = $"{UserRoles.ProcurementOfficer},{UserRoles.Approver}")]
public class RfqRoundEvaluationController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly IEvaluationDecisionService _evaluationDecisionService;

    public RfqRoundEvaluationController(
        IEvaluationService evaluationService,
        IEvaluationDecisionService evaluationDecisionService)
    {
        _evaluationService = evaluationService;
        _evaluationDecisionService = evaluationDecisionService;
    }

    [HttpPost("evaluate")]
    [Authorize(Roles = UserRoles.ProcurementOfficer)]
    [ProducesResponseType(typeof(EvaluateRoundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EvaluateRoundResponse>> Evaluate(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _evaluationService.EvaluateRoundAsync(
            rfqId, roundId, userId, GetClientIpAddress(), cancellationToken);

        return Ok(result);
    }

    [HttpGet("evaluation")]
    [ProducesResponseType(typeof(EvaluationResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluationResultsResponse>> GetResults(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        var result = await _evaluationService.GetResultsAsync(rfqId, roundId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("evaluation/decision")]
    [Authorize(Roles = UserRoles.Approver)]
    [ProducesResponseType(typeof(EvaluationDecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EvaluationDecisionResponse>> SubmitDecision(
        long rfqId,
        long roundId,
        [FromBody] EvaluationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _evaluationDecisionService.SubmitDecisionAsync(
            rfqId, roundId, userId, request, GetClientIpAddress(), cancellationToken);

        return Ok(result);
    }

    [HttpGet("evaluation/decision")]
    [ProducesResponseType(typeof(EvaluationDecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluationDecisionResponse>> GetDecision(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        var result = await _evaluationDecisionService.GetDecisionAsync(rfqId, roundId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("evaluation/{bidId:long}")]
    [ProducesResponseType(typeof(EvaluationBidDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluationBidDetailResponse>> GetBidDetail(
        long rfqId,
        long roundId,
        long bidId,
        CancellationToken cancellationToken)
    {
        var result = await _evaluationService.GetBidDetailAsync(rfqId, roundId, bidId, cancellationToken);
        return Ok(result);
    }

    private string? GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        return ipAddress is "::1" ? IPAddress.Loopback.ToString() : ipAddress;
    }
}