using ACTApi.DTOs;
using ACTApi.Infrastructure;
using ACTApi.Mappers;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Domain service for ACT door and user group management.</summary>
public class GroupService : IGroupService
{
    private readonly ILogger<GroupService> _logger;

    /// <summary>Initializes a new instance of <see cref="GroupService"/>.</summary>
    public GroupService(ILogger<GroupService> logger)
    {
        _logger = logger;
    }

    // ── Door Groups ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<List<GroupDto>> GetDoorGroupsAsync(ActEnterprisePublicAPI_ExtClient proxy)
    {
        var groups = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetDoorGroupsAsync(0, 1000, true),
            "GetDoorGroups",
            _logger);

        return groups?.Select(g => g.ToDto()).ToList() ?? new List<GroupDto>();
    }

    /// <inheritdoc />
    public async Task<GroupDto?> GetDoorGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, int id)
    {
        var group = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetDoorGroupAsync(id),
            "GetDoorGroup",
            _logger);

        return group?.ToDto();
    }

    /// <inheritdoc />
    public async Task<int> CreateDoorGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, GroupDto group)
    {
        var wcfGroup = new DoorGroupValue
        {
            DoorGroupNumber = group.Index,
            Name = group.Name,
            MemberDoors = Array.Empty<int>(),
            DoorMasks = new Dictionary<int, ushort>(),
            IsValid = true
        };

        var id = await WcfCallLogger.ExecuteAsync(
            () => proxy.InsertDoorGroupAsync(wcfGroup),
            "InsertDoorGroup",
            _logger);

        _logger.LogInformation("Created door group {GroupId} ({Name})", id, group.Name);
        return id;
    }

    /// <inheritdoc />
    public async Task UpdateDoorGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, GroupDto group)
    {
        var existing = await proxy.GetDoorGroupAsync(group.Index);
        if (existing == null)
            throw new InvalidOperationException($"Door group {group.Index} not found.");

        existing.Name = group.Name;
        existing.IsValid = true;

        await WcfCallLogger.ExecuteAsync(
            () => proxy.UpdateDoorGroupAsync(existing),
            "UpdateDoorGroup",
            _logger);

        _logger.LogInformation("Updated door group {GroupId} ({Name})", group.Index, group.Name);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDoorGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, int id)
    {
        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.DeleteDoorGroupAsync(id),
            "DeleteDoorGroup",
            _logger);

        _logger.LogInformation("Deleted door group {GroupId} — result: {Result}", id, result);
        return result;
    }

    // ── User Groups ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<List<GroupDto>> GetUserGroupsAsync(ActEnterprisePublicAPI_ExtClient proxy)
    {
        var groups = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetUserGroupsAsync(new Dictionary<string, string>(), 0, 1000, true),
            "GetUserGroups",
            _logger);

        return groups?.Select(g => g.ToDto()).ToList() ?? new List<GroupDto>();
    }

    /// <inheritdoc />
    public async Task<GroupDto?> GetUserGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, int id)
    {
        var group = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetUserGroupAsync(id, true, false),
            "GetUserGroup",
            _logger);

        return group?.ToDto();
    }

    /// <inheritdoc />
    public async Task<int> CreateUserGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, GroupDto group)
    {
        var wcfGroup = new UserGroupValue
        {
            UserGroupNumber = 0, // auto-assign
            UserGroupName = group.Name,
            IsValid = true,
            Enabled = true
        };

        var id = await WcfCallLogger.ExecuteAsync(
            () => proxy.InsertUserGroupAsync(wcfGroup),
            "InsertUserGroup",
            _logger);

        _logger.LogInformation("Created user group {GroupId} ({Name})", id, group.Name);
        return id;
    }

    /// <inheritdoc />
    public async Task UpdateUserGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, GroupDto group)
    {
        var existing = await proxy.GetUserGroupAsync(group.Index, true, false);
        if (existing == null)
            throw new InvalidOperationException($"User group {group.Index} not found.");

        existing.UserGroupName = group.Name;
        existing.IsValid = true;

        await WcfCallLogger.ExecuteAsync(
            () => proxy.UpdateUserGroupAsync(existing),
            "UpdateUserGroup",
            _logger);

        _logger.LogInformation("Updated user group {GroupId} ({Name})", group.Index, group.Name);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, int id)
    {
        await WcfCallLogger.ExecuteAsync(
            () => proxy.DeleteUserGroupAsync(id),
            "DeleteUserGroup",
            _logger);

        _logger.LogInformation("Deleted user group {GroupId}", id);
        return true;
    }
}
