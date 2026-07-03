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
    public async Task<PaginatedResponse<DoorDto>> GetDoorsAsync(
        ActEnterprisePublicAPI_ExtClient proxy, int page = 1, int pageSize = 200)
    {
        // Fetch all doors using the cursor pattern from the ACT spec:
        // loop with next=true, advancing by GlobalDoorNumber
        var allDoors = await WcfPaginationHelper.GetAllAsync<DoorValueExt>(
            (start, max) => proxy.GetDoorsAsync(start, max, true, true),
            d => d.GlobalDoorNumber,
            pageSize);

        // Apply in-memory pagination
        var totalCount = allDoors.Count;
        var pagedItems = allDoors
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => d.ToDto())
            .ToList();

        return new PaginatedResponse<DoorDto>
        {
            Data = pagedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = (page * pageSize) < totalCount
        };
    }

    /// <inheritdoc />
    public async Task<DoorDto?> GetDoorAsync(
        ActEnterprisePublicAPI_ExtClient proxy, int globalDoorNumber)
    {
        // Fetch all doors using proper ACT pagination, find matching door
        var allDoors = await WcfPaginationHelper.GetAllAsync<DoorValueExt>(
            (start, max) => proxy.GetDoorsAsync(start, max, true, true),
            d => d.GlobalDoorNumber,
            200);

        var match = allDoors.FirstOrDefault(d => d.GlobalDoorNumber == globalDoorNumber);
        if (match == null)
        {
            _logger.LogWarning("Door {GlobalDoorNumber} not found", globalDoorNumber);
            return null;
        }

        return match.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> IssueDoorCommandAsync(
        ActEnterprisePublicAPI_ExtClient proxy, int globalDoorNumber, byte command)
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
    public async Task<bool> IssueDoorCommandBatchAsync(
        ActEnterprisePublicAPI_ExtClient proxy, int[] globalDoorNumbers, byte command)
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
