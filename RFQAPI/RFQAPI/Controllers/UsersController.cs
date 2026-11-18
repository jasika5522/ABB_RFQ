using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Users;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var profile = await _userService.GetByIdAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Approver)]
    [ProducesResponseType(typeof(PagedUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedUsersResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:long}/active")]
    [Authorize(Roles = UserRoles.Approver)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateActiveStatus(
        long id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actingUserId = User.GetUserId();
        var updated = await _userService.UpdateActiveStatusAsync(
            id,
            actingUserId,
            request.IsActive,
            cancellationToken);

        return Ok(updated);
    }
}