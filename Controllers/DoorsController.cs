using ACTApi.DTOs;
using ACTApi.Services;
using ACTServiceReference;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// Door listing and command endpoints. Supports single and batch door operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DoorsController : ControllerBase
{
    private readonly ILogger<DoorsController> _logger;
    private readonly IACTProServices _actProServices;
    private readonly IDoorService _doorService;

    /// <summary>Initializes a new instance of <see cref="DoorsController"/>.</summary>
    public DoorsController(
        ILogger<DoorsController> logger,
        IACTProServices actProServices,
        IDoorService doorService)
    {
        _logger = logger;
        _actProServices = actProServices;
        _doorService = doorService;
    }

    /// <summary>Gets a paginated list of doors.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<DoorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDoors([FromQuery] int page = 1, [FromQuery] int pageSize = 200)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _doorService.GetDoorsAsync(_actProServices.CurrentProxy!, page, pageSize);
            return Ok(result);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Gets a single door by global door number.</summary>
    /// <param name="globalDoorNumber">The global door number.</param>
    [HttpGet("{globalDoorNumber}")]
    [ProducesResponseType(typeof(DoorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDoor(int globalDoorNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var door = await _doorService.GetDoorAsync(_actProServices.CurrentProxy!, globalDoorNumber);
            if (door == null)
                return NotFound(new { Message = $"Door {globalDoorNumber} not found." });

            return Ok(door);
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Locks a door.</summary>
    /// <param name="globalDoorNumber">The global door number.</param>
    [HttpPost("{globalDoorNumber}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> LockDoor(int globalDoorNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _doorService.IssueDoorCommandAsync(
                _actProServices.CurrentProxy!, globalDoorNumber, (byte)DoorCommands.LockDoor);
            return Ok(new { Message = $"Door {globalDoorNumber} locked.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Unlocks a door.</summary>
    /// <param name="globalDoorNumber">The global door number.</param>
    [HttpPost("{globalDoorNumber}/unlock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UnlockDoor(int globalDoorNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _doorService.IssueDoorCommandAsync(
                _actProServices.CurrentProxy!, globalDoorNumber, (byte)DoorCommands.UnlockDoor);
            return Ok(new { Message = $"Door {globalDoorNumber} unlocked.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Activates the relay (momentary access) on a door.</summary>
    /// <param name="globalDoorNumber">The global door number.</param>
    [HttpPost("{globalDoorNumber}/access")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AccessDoor(int globalDoorNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _doorService.IssueDoorCommandAsync(
                _actProServices.CurrentProxy!, globalDoorNumber, (byte)DoorCommands.ActivateRelay);
            return Ok(new { Message = $"Access granted for door {globalDoorNumber}.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Normalizes a door (restores to default state).</summary>
    /// <param name="globalDoorNumber">The global door number.</param>
    [HttpPost("{globalDoorNumber}/normalize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> NormalizeDoor(int globalDoorNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _doorService.IssueDoorCommandAsync(
                _actProServices.CurrentProxy!, globalDoorNumber, (byte)DoorCommands.Normalize);
            return Ok(new { Message = $"Door {globalDoorNumber} normalized.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Issues a batch command on multiple doors.</summary>
    /// <param name="request">The batch command request with door numbers and command.</param>
    [HttpPost("batch-command")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> BatchCommand([FromBody] DoorBatchCommandRequest request)
    {
        if (request.GlobalDoorNumbers == null || request.GlobalDoorNumbers.Count == 0)
            return BadRequest(new { Message = "At least one door number is required." });

        await _actProServices.CreateProxy();
        try
        {
            var result = await _doorService.IssueDoorCommandBatchAsync(
                _actProServices.CurrentProxy!,
                request.GlobalDoorNumbers.ToArray(),
                request.Command);
            return Ok(new { Message = $"Batch command {request.Command} issued on {request.GlobalDoorNumbers.Count} doors.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }
}

/// <summary>Request model for batch door commands.</summary>
public class DoorBatchCommandRequest
{
    /// <summary>Door command (0=Normalize, 1=ActivateRelay, 2=LockDoor, 3=UnlockDoor).</summary>
    public byte Command { get; set; }

    /// <summary>List of global door numbers.</summary>
    public List<int> GlobalDoorNumbers { get; set; } = new();
}
