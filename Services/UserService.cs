using ACTApi.DTOs;
using ACTApi.Infrastructure;
using ACTApi.Mappers;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Domain service for ACT user CRUD operations.</summary>
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    /// <summary>Initializes a new instance of <see cref="UserService"/>.</summary>
    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetUserAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber)
    {
        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetUserAsync(userNumber, true, true, false),
            "GetUser",
            _logger);

        return result is { IsValid: true } ? result.ToDto() : null;
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<UserDto>> GetUsersAsync(ActEnterprisePublicAPI_ExtClient proxy, UserSearchRequest? request)
    {
        request ??= new UserSearchRequest();
        var matchers = new Dictionary<string, string>();
        var exactMatchers = new Dictionary<string, int>();

        if (!string.IsNullOrWhiteSpace(request.Forename))
            matchers["Forename"] = request.Forename;
        if (!string.IsNullOrWhiteSpace(request.Surname))
            matchers["Surname"] = request.Surname;
        if (request.CardNumber.HasValue)
            exactMatchers["CardNumber"] = (int)request.CardNumber.Value;
        if (request.Group.HasValue)
            exactMatchers["UserGroup"] = request.Group.Value;

        return await WcfPaginationHelper.GetPageAsync<UserValueExt, UserDto>(
            (start, max) => proxy.GetUsersAsync(matchers, exactMatchers, start, start + max, max, true, request.Enabled ?? false, 0),
            request.Page,
            request.PageSize,
            u => u.ToDto());
    }

    /// <inheritdoc />
    public async Task<int> CreateUserAsync(ActEnterprisePublicAPI_ExtClient proxy, UserDto user)
    {
        var wcfUser = user.ToWcf();
        var userNumber = await WcfCallLogger.ExecuteAsync(
            () => proxy.InsertUserAsync(wcfUser, true),
            "InsertUser",
            _logger);

        _logger.LogInformation(
            "Created user {UserNumber} ({Forename} {Surname})",
            userNumber, user.Forename, user.Surname);

        return userNumber;
    }

    /// <inheritdoc />
    public async Task UpdateUserAsync(ActEnterprisePublicAPI_ExtClient proxy, UserDto user)
    {
        var wcfUser = user.ToWcf();
        await WcfCallLogger.ExecuteAsync(
            () => proxy.UpdateUserAsync(wcfUser),
            "UpdateUser",
            _logger);

        _logger.LogInformation(
            "Updated user {UserNumber} ({Forename} {Surname})",
            user.UserNumber, user.Forename, user.Surname);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber)
    {
        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.DeleteUserAsync(userNumber),
            "DeleteUser",
            _logger);

        if (result)
        {
            _logger.LogInformation("Deleted user {UserNumber}", userNumber);
        }
        else
        {
            _logger.LogWarning("DeleteUser returned false for user {UserNumber}", userNumber);
        }

        return result;
    }
}
