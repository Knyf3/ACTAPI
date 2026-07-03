using ACTApi.DTOs;
using ACTApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// Extra rights (door group / timezone overrides) and door plan management per user.
/// </summary>
[ApiController]
[Route("api/users/{userNumber}")]
[Produces("application/json")]
public class ExtraRightsController : ControllerBase
{
    private readonly ILogger<ExtraRightsController> _logger;
    private readonly IACTProServices _actProServices;
    private readonly IExtraRightsService _extraRightsService;

    /// <summary>Initializes a new instance of <see cref="ExtraRightsController"/>.</summary>
    public ExtraRightsController(
        ILogger<ExtraRightsController> logger,
        IACTProServices actProServices,
        IExtraRightsService extraRightsService)
    {
        _logger = logger;
        _actProServices = actProServices;
        _extraRightsService = extraRightsService;
    }

    /// <summary>Gets extra rights for a user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    [HttpGet("extra-rights")]
    [ProducesResponseType(typeof(ExtraRightsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetExtraRights(int userNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var rights = await _extraRightsService.GetExtraRightsAsync(_actProServices.CurrentProxy!, userNumber);
            if (rights == null)
                return NotFound(new { Message = $"Extra rights for user {userNumber} not found." });

            return Ok(rights);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Updates extra rights for a user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    /// <param name="rights">Extra rights data.</param>
    [HttpPut("extra-rights")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateExtraRights(int userNumber, [FromBody] ExtraRightsDto rights)
    {
        await _actProServices.CreateProxy();
        try
        {
            await _extraRightsService.UpdateExtraRightsAsync(_actProServices.CurrentProxy!, userNumber, rights);
            return Ok(new { Message = $"Extra rights for user {userNumber} updated." });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Deletes all extra rights for a user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    [HttpDelete("extra-rights")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteExtraRights(int userNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _extraRightsService.DeleteExtraRightsAsync(_actProServices.CurrentProxy!, userNumber);
            return Ok(new { Message = $"Extra rights for user {userNumber} deleted.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Gets the door plan for a user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    [HttpGet("door-plan")]
    [ProducesResponseType(typeof(DoorPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDoorPlan(int userNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var plan = await _extraRightsService.GetDoorPlanAsync(_actProServices.CurrentProxy!, userNumber);
            if (plan == null)
                return NotFound(new { Message = $"Door plan for user {userNumber} not found." });

            return Ok(plan);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Updates the door plan for a user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    /// <param name="plan">Door plan data.</param>
    [HttpPut("door-plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UpdateDoorPlan(int userNumber, [FromBody] DoorPlanDto plan)
    {
        await _actProServices.CreateProxy();
        try
        {
            await _extraRightsService.UpdateDoorPlanAsync(_actProServices.CurrentProxy!, userNumber, plan);
            return Ok(new { Message = $"Door plan for user {userNumber} updated." });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Deletes the door plan for a user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    [HttpDelete("door-plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteDoorPlan(int userNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _extraRightsService.DeleteDoorPlanAsync(_actProServices.CurrentProxy!, userNumber);
            return Ok(new { Message = $"Door plan for user {userNumber} deleted.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }
}
