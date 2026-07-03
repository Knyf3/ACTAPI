using ACTApi.DTOs;
using ACTApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// CRUD operations for ACT door groups and user groups.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GroupsController : ControllerBase
{
    private readonly ILogger<GroupsController> _logger;
    private readonly IACTProServices _actProServices;
    private readonly IGroupService _groupService;

    /// <summary>Initializes a new instance of <see cref="GroupsController"/>.</summary>
    public GroupsController(
        ILogger<GroupsController> logger,
        IACTProServices actProServices,
        IGroupService groupService)
    {
        _logger = logger;
        _actProServices = actProServices;
        _groupService = groupService;
    }

    // ── Door Groups ─────────────────────────────────────────────────

    /// <summary>Gets all door groups.</summary>
    [HttpGet("door")]
    [ProducesResponseType(typeof(List<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDoorGroups()
    {
        await _actProServices.CreateProxy();
        try
        {
            var groups = await _groupService.GetDoorGroupsAsync(_actProServices.CurrentProxy!);
            return Ok(groups);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Gets a single door group.</summary>
    /// <param name="id">Door group number.</param>
    [HttpGet("door/{id}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDoorGroup(int id)
    {
        await _actProServices.CreateProxy();
        try
        {
            var group = await _groupService.GetDoorGroupAsync(_actProServices.CurrentProxy!, id);
            if (group == null)
                return NotFound(new { Message = $"Door group {id} not found." });

            return Ok(group);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Creates a new door group.</summary>
    /// <param name="group">Door group data.</param>
    [HttpPost("door")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateDoorGroup([FromBody] GroupDto group)
    {
        await _actProServices.CreateProxy();
        try
        {
            var id = await _groupService.CreateDoorGroupAsync(_actProServices.CurrentProxy!, group);
            group.Index = id;
            return CreatedAtAction(nameof(GetDoorGroup), new { id }, group);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Updates an existing door group.</summary>
    /// <param name="id">Door group number.</param>
    /// <param name="group">Updated door group data.</param>
    [HttpPut("door/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateDoorGroup(int id, [FromBody] GroupDto group)
    {
        if (id != group.Index)
            return BadRequest(new { Message = "Group ID in URL does not match body." });

        await _actProServices.CreateProxy();
        try
        {
            await _groupService.UpdateDoorGroupAsync(_actProServices.CurrentProxy!, group);
            return Ok(new { Message = $"Door group {id} updated." });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Deletes a door group.</summary>
    /// <param name="id">Door group number.</param>
    [HttpDelete("door/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteDoorGroup(int id)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _groupService.DeleteDoorGroupAsync(_actProServices.CurrentProxy!, id);
            return Ok(new { Message = $"Door group {id} deleted.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    // ── User Groups ─────────────────────────────────────────────────

    /// <summary>Gets all user groups.</summary>
    [HttpGet("user")]
    [ProducesResponseType(typeof(List<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetUserGroups()
    {
        await _actProServices.CreateProxy();
        try
        {
            var groups = await _groupService.GetUserGroupsAsync(_actProServices.CurrentProxy!);
            return Ok(groups);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Gets a single user group.</summary>
    /// <param name="id">User group number.</param>
    [HttpGet("user/{id}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetUserGroup(int id)
    {
        await _actProServices.CreateProxy();
        try
        {
            var group = await _groupService.GetUserGroupAsync(_actProServices.CurrentProxy!, id);
            if (group == null)
                return NotFound(new { Message = $"User group {id} not found." });

            return Ok(group);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Creates a new user group.</summary>
    /// <param name="group">User group data.</param>
    [HttpPost("user")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateUserGroup([FromBody] GroupDto group)
    {
        await _actProServices.CreateProxy();
        try
        {
            var id = await _groupService.CreateUserGroupAsync(_actProServices.CurrentProxy!, group);
            group.Index = id;
            return CreatedAtAction(nameof(GetUserGroup), new { id }, group);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Updates an existing user group.</summary>
    /// <param name="id">User group number.</param>
    /// <param name="group">Updated user group data.</param>
    [HttpPut("user/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateUserGroup(int id, [FromBody] GroupDto group)
    {
        if (id != group.Index)
            return BadRequest(new { Message = "Group ID in URL does not match body." });

        await _actProServices.CreateProxy();
        try
        {
            await _groupService.UpdateUserGroupAsync(_actProServices.CurrentProxy!, group);
            return Ok(new { Message = $"User group {id} updated." });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Deletes a user group.</summary>
    /// <param name="id">User group number.</param>
    [HttpDelete("user/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteUserGroup(int id)
    {
        await _actProServices.CreateProxy();
        try
        {
            await _groupService.DeleteUserGroupAsync(_actProServices.CurrentProxy!, id);
            return Ok(new { Message = $"User group {id} deleted." });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }
}
