using ACTApi.DTOs;
using ACTApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// Muster (user tracking) endpoints — who's on site, who's absent, and reset.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MusterController : ControllerBase
{
    private readonly ILogger<MusterController> _logger;
    private readonly IACTProServices _actProServices;
    private readonly IMusterService _musterService;

    /// <summary>Initializes a new instance of <see cref="MusterController"/>.</summary>
    public MusterController(
        ILogger<MusterController> logger,
        IACTProServices actProServices,
        IMusterService musterService)
    {
        _logger = logger;
        _actProServices = actProServices;
        _musterService = musterService;
    }

    /// <summary>Gets the current muster report (users tracked on site today).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserTrackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetMuster()
    {
        await _actProServices.CreateProxy();
        try
        {
            var muster = await _musterService.GetMusterAsync(_actProServices.CurrentProxy!);
            return Ok(muster);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Gets the list of absent users for today.</summary>
    [HttpGet("absent")]
    [ProducesResponseType(typeof(List<LogEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAbsentUsers()
    {
        await _actProServices.CreateProxy();
        try
        {
            var absent = await _musterService.GetAbsentUsersAsync(_actProServices.CurrentProxy!);
            return Ok(absent);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Resets all muster data.</summary>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ResetMuster()
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _musterService.ResetMusterAsync(_actProServices.CurrentProxy!);
            return Ok(new { Message = "Muster reset completed.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }
}
