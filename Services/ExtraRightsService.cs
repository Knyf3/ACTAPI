using ACTApi.DTOs;
using ACTApi.Infrastructure;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Domain service for ACT extra rights and door plan management.</summary>
public class ExtraRightsService : IExtraRightsService
{
    private readonly ILogger<ExtraRightsService> _logger;

    /// <summary>Initializes a new instance of <see cref="ExtraRightsService"/>.</summary>
    public ExtraRightsService(ILogger<ExtraRightsService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ExtraRightsDto?> GetExtraRightsAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber)
    {
        var rights = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetExtraRightsAsync(userNumber),
            "GetExtraRights",
            _logger);

        if (rights == null || !rights.IsValid)
            return null;

        var dto = new ExtraRightsDto
        {
            UserNumber = rights.UserNumber,
            Rights = (rights.Rights ?? Array.Empty<ExtraRights>())
                .Select(r => new ExtraRightEntryDto
                {
                    DoorGroup = r.DoorGroup,
                    Timezone = r.Timezone,
                    ValidityFrom = r.ValidityFrom,
                    ValidityTo = r.ValidityTo
                })
                .ToList()
        };

        return dto;
    }

    /// <inheritdoc />
    public async Task UpdateExtraRightsAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber, ExtraRightsDto dto)
    {
        var rightsArray = dto.Rights
            .Select(r => new ExtraRights
            {
                DoorGroup = r.DoorGroup,
                Timezone = r.Timezone,
                ValidityFrom = r.ValidityFrom,
                ValidityTo = r.ValidityTo
            })
            .ToArray();

        var wcfRights = new ExtraRightsValue
        {
            UserNumber = userNumber,
            Rights = rightsArray,
            IsValid = true
        };

        await WcfCallLogger.ExecuteAsync(
            () => proxy.UpdateExtraRightsAsync(wcfRights),
            "UpdateExtraRights",
            _logger);

        _logger.LogInformation(
            "Updated extra rights for user {UserNumber} ({Count} entries)",
            userNumber, rightsArray.Length);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteExtraRightsAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber)
    {
        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.DeleteExtraRightsAsync(userNumber),
            "DeleteExtraRights",
            _logger);

        _logger.LogInformation(
            "Deleted extra rights for user {UserNumber} — result: {Result}",
            userNumber, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<DoorPlanDto?> GetDoorPlanAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber)
    {
        var plan = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetDoorPlanAsync(userNumber),
            "GetDoorPlan",
            _logger);

        if (plan == null || !plan.IsValid)
            return null;

        var dto = new DoorPlanDto
        {
            UserNumber = plan.UserNumber,
            Plans = (plan.Plans ?? Array.Empty<DoorPlan>())
                .Select(p => new DoorPlanEntryDto
                {
                    Timezone = p.Timezone,
                    Doors = (p.doors ?? Array.Empty<int>()).ToList()
                })
                .ToList()
        };

        return dto;
    }

    /// <inheritdoc />
    public async Task UpdateDoorPlanAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber, DoorPlanDto dto)
    {
        var plansArray = dto.Plans
            .Select(p => new DoorPlan
            {
                Timezone = p.Timezone,
                doors = p.Doors.ToArray()
            })
            .ToArray();

        var wcfPlan = new DoorPlanValue
        {
            UserNumber = userNumber,
            Plans = plansArray,
            IsValid = true
        };

        await WcfCallLogger.ExecuteAsync(
            () => proxy.UpdateDoorPlanAsync(wcfPlan),
            "UpdateDoorPlan",
            _logger);

        _logger.LogInformation(
            "Updated door plan for user {UserNumber} ({Count} plans)",
            userNumber, plansArray.Length);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDoorPlanAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber)
    {
        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.DeleteDoorPlanAsync(userNumber),
            "DeleteDoorPlan",
            _logger);

        _logger.LogInformation(
            "Deleted door plan for user {UserNumber} — result: {Result}",
            userNumber, result);

        return result;
    }
}
