using ACTApi.DTOs;
using ACTApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// CRUD operations for ACT users. Each request establishes a fresh WCF session.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IACTProServices _actProServices;
    private readonly IUserService _userService;

    /// <summary>Initializes a new instance of <see cref="UsersController"/>.</summary>
    public UsersController(
        ILogger<UsersController> logger,
        IACTProServices actProServices,
        IUserService userService)
    {
        _logger = logger;
        _actProServices = actProServices;
        _userService = userService;
    }

    /// <summary>Gets a paginated, filterable list of users.</summary>
    /// <param name="forename">Filter by forename (partial match).</param>
    /// <param name="surname">Filter by surname (partial match).</param>
    /// <param name="group">Filter by user group number.</param>
    /// <param name="cardNumber">Filter by card number.</param>
    /// <param name="enabled">Filter by enabled status.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? forename = null,
        [FromQuery] string? surname = null,
        [FromQuery] int? group = null,
        [FromQuery] uint? cardNumber = null,
        [FromQuery] bool? enabled = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        await _actProServices.CreateProxy();
        try
        {
            var request = new UserSearchRequest
            {
                Forename = forename,
                Surname = surname,
                Group = group,
                CardNumber = cardNumber,
                Enabled = enabled,
                Page = page,
                PageSize = pageSize
            };

            var result = await _userService.GetUsersAsync(_actProServices.CurrentProxy!, request);
            return Ok(result);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Gets a single user by user number.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    [HttpGet("{userNumber}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetUser(int userNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var user = await _userService.GetUserAsync(_actProServices.CurrentProxy!, userNumber);
            if (user == null)
                return NotFound(new { Message = $"User {userNumber} not found." });

            return Ok(user);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Creates a new user.</summary>
    /// <param name="user">User data.</param>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateUser([FromBody] UserDto user)
    {
        await _actProServices.CreateProxy();
        try
        {
            var id = await _userService.CreateUserAsync(_actProServices.CurrentProxy!, user);
            user.UserNumber = id;
            return CreatedAtAction(nameof(GetUser), new { userNumber = id }, user);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Updates an existing user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    /// <param name="user">Updated user data.</param>
    [HttpPut("{userNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateUser(int userNumber, [FromBody] UserDto user)
    {
        if (userNumber != user.UserNumber)
            return BadRequest(new { Message = "User number in URL does not match body." });

        await _actProServices.CreateProxy();
        try
        {
            await _userService.UpdateUserAsync(_actProServices.CurrentProxy!, user);
            return Ok(new { Message = $"User {userNumber} updated." });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Deletes a user by user number.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    [HttpDelete("{userNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteUser(int userNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _userService.DeleteUserAsync(_actProServices.CurrentProxy!, userNumber);
            if (!result)
                return NotFound(new { Message = $"User {userNumber} not found or could not be deleted." });

            return Ok(new { Message = $"User {userNumber} deleted." });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }
}
