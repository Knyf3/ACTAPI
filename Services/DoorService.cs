using ACTApi.DTOs;
using ACTApi.Infrastructure;
using ACTApi.Mappers;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Domain service for ACT door listing and commands.</summary>
public class DoorService : IDoorService
{
    private readonly ILogger<DoorService> _logger;

    /// <summary>Initializes a new instance of <see cref="DoorService"/>.</summary>
    public DoorService(ILogger<DoorService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<DoorDto>> GetDoorsAsync(ActEnterprisePublicAPI_ExtClient proxy, int page = 1, int pageSize = 200)
    {
        return await WcfPaginationHelper.GetPageAsync<DoorValueExt, DoorDto>(
            (start, max) => proxy.GetDoorsAsync(0, max, false, true),
            page,
            pageSize,
            d => d.ToDto());
    }

    /// <inheritdoc />
    public async Task<DoorDto?> GetDoorAsync(ActEnterprisePublicAPI_ExtClient proxy, int globalDoorNumber)
    {
        // GetDoorsAsync with systemIndex=0, max=1 to fetch the specific door
        var doors = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetDoorsAsync(0, 1, false, true),
            "GetDoors",
            _logger);

        if (doors == null || doors.Length == 0)
            return null;

        // Find the specific door
        foreach (var door in doors)
        {
            if (door.GlobalDoorNumber == globalDoorNumber)
                return door.ToDto();
        }

        // If not found in first page, try with more
        var allDoors = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetDoorsAsync(0, 1000, false, true),
            "GetDoors",
            _logger);

        var match = allDoors?.FirstOrDefault(d => d.GlobalDoorNumber == globalDoorNumber);
        return match?.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> IssueDoorCommandAsync(ActEnterprisePublicAPI_ExtClient proxy, int globalDoorNumber, byte command)
    {
        var cmd = command.ToCommandExt();
        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.IssueCommandOnDoorsAsync(cmd, [globalDoorNumber]),
            "IssueCommandOnDoors",
            _logger);

        _logger.LogInformation(
            "Issued command {Command} on door {GlobalDoorNumber} — result: {Result}",
            command, globalDoorNumber, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IssueDoorCommandBatchAsync(ActEnterprisePublicAPI_ExtClient proxy, int[] globalDoorNumbers, byte command)
    {
        if (globalDoorNumbers == null || globalDoorNumbers.Length == 0)
            throw new ArgumentException("At least one door number is required.", nameof(globalDoorNumbers));

        var cmd = command.ToCommandExt();
        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.IssueCommandOnDoorsAsync(cmd, globalDoorNumbers),
            "IssueCommandOnDoors",
            _logger);

        _logger.LogInformation(
            "Issued command {Command} on {Count} doors — result: {Result}",
            command, globalDoorNumbers.Length, result);

        return result;
    }
}
