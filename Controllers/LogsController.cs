using ACTApi.DTOs;
using ACTApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// Log event query endpoints — filter by date range, event type, user, or door.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;
    private readonly IACTProServices _actProServices;
    private readonly ILogService _logService;

    /// <summary>Initializes a new instance of <see cref="LogsController"/>.</summary>
    public LogsController(
        ILogger<LogsController> logger,
        IACTProServices actProServices,
        ILogService logService)
    {
        _logger = logger;
        _actProServices = actProServices;
        _logService = logService;
    }

    /// <summary>Queries log events with optional filters.</summary>
    /// <param name="from">Start date (default: 24 hours ago).</param>
    /// <param name="to">End date (default: now).</param>
    /// <param name="eventType">Filter by event type code.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<LogEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] uint? eventType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _logService.GetLogsAsync(
                _actProServices.CurrentProxy!, from, to, eventType, page, pageSize);
            return Ok(result);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Gets log events for a specific user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    /// <param name="from">Start date.</param>
    /// <param name="to">End date.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    [HttpGet("user/{userNumber}")]
    [ProducesResponseType(typeof(PaginatedResponse<LogEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetLogsByUser(
        int userNumber,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _logService.GetLogsByUserAsync(
                _actProServices.CurrentProxy!, userNumber, from, to, page, pageSize);
            return Ok(result);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Gets log events for a specific door.</summary>
    /// <param name="globalDoorNumber">The global door number.</param>
    /// <param name="from">Start date.</param>
    /// <param name="to">End date.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    [HttpGet("door/{globalDoorNumber}")]
    [ProducesResponseType(typeof(PaginatedResponse<LogEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetLogsByDoor(
        int globalDoorNumber,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _logService.GetLogsByDoorAsync(
                _actProServices.CurrentProxy!, globalDoorNumber, from, to, page, pageSize);
            return Ok(result);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }
}
