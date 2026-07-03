# ACT Enterprise WCF API → REST API: Architecture Assessment

**Project:** ACTApi  
**Location:** `~/projects/ACTApi`  
**Date:** 2026-07-03  
**Target:** .NET 8 Windows Service wrapping ACT Enterprise WCF API

---

## 1. Project Structure & Folder Layout

### Current State
```
ACTApi/
├── Program.cs                          # Entry point + DI + Service config
├── ACTApi.csproj                       # .NET 8 Web SDK, WCF/Swagger/Serilog pkgs
├── ACTApi.sln / ACTApi.slnx
├── appsettings.json                    # Serilog log levels only
├── ACTApi.http                         # Dev test file (weatherforecast stub)
├── WeatherForecast.cs                  # Template boilerplate — REMOVE
├── ACTProAPISetup.iss                  # InnoSetup installer (311 lines, mature)
├── Controllers/
│   ├── AccessController.cs             # POST api/access/allowaccess
│   └── WeatherForecastController.cs    # Template boilerplate — REMOVE
├── Services/
│   ├── IACTProServices.cs              # Interface: CreateProxy, CloseProxy, AllowAccess
│   └── ACTProServices.cs               # WCF session + single door command
├── Helpers/
│   └── SettingsHelper.cs               # Raw JSON file reader (Newtonsoft)
├── Settings/
│   └── Settings.json                   # ACT server credentials (plaintext)
├── Connected Services/
│   └── ACTServiceReference/
│       ├── ConnectedService.json       # svcutil config (mex endpoint)
│       └── Reference.cs                # 10.5K lines auto-generated WCF proxy
├── bin/ & obj/                         # Build artifacts
└── Logs/                               # Runtime log output
```

### Recommended Expansion
```
ACTApi/
├── Program.cs
├── ACTApi.csproj
├── appsettings.json                    # Merge Settings.json into here
├── appsettings.Development.json        # Dev overrides
├── appsettings.Production.json         # Prod overrides
├── ACTProAPISetup.iss
├── Controllers/
│   ├── AccessController.cs             # Door-level commands (lock/unlock/access)
│   ├── UsersController.cs              # User CRUD
│   ├── DoorsController.cs              # Door listing
│   ├── GroupsController.cs             # Door groups, user groups
│   ├── ExtraRightsController.cs        # Extra rights & door plans
│   ├── PhotosController.cs             # User photo get/set
│   ├── MusterController.cs             # Muster reports
│   ├── LogsController.cs               # Log event queries
│   └── ImportExportController.cs       # CSV import/export
├── Services/
│   ├── IACTProServices.cs              # Core session management interface
│   ├── ACTProServices.cs               # Session lifecycle + core operations
│   ├── IUserService.cs
│   ├── UserService.cs                  # User CRUD + search
│   ├── IDoorService.cs
│   ├── DoorService.cs                  # Door listing + commands
│   ├── IGroupService.cs
│   ├── GroupService.cs                 # Door/user/elevator/floor groups
│   ├── IPhotoService.cs
│   ├── PhotoService.cs                 # Chunked photo transfer
│   ├── IExtraRightsService.cs
│   ├── ExtraRightsService.cs           # Extra rights + door plans
│   ├── IMusterService.cs
│   ├── MusterService.cs                # Muster/on-site reports
│   ├── ILogService.cs
│   └── LogService.cs                   # Log event queries
├── Models/
│   ├── ActSession.cs                   # Session state DTO (proxy + state)
│   └── ErrorResponse.cs                # Standardized error envelope
├── DTOs/
│   ├── UserDto.cs                      # Clean API representation of UserValueExt
│   ├── DoorDto.cs                      # Clean API rep of DoorValueExt
│   ├── DoorCommandRequest.cs           # Command input model
│   ├── GroupDto.cs                     # Clean rep of DoorGroupValue etc.
│   ├── LogEventDto.cs                  # Clean rep of LogValueExt
│   ├── UserTrackDto.cs                 # Clean rep of UserTrackValueExt
│   ├── ExtraRightsDto.cs               # Clean rep of ExtraRightsValue
│   ├── DoorPlanDto.cs                  # Clean rep of DoorPlanValue
│   ├── PhotoDto.cs                     # Photo chunk metadata
│   ├── PaginatedResponse.cs            # Generic paginated wrapper
│   └── SearchRequest.cs                # User search input model
├── Mappers/
│   ├── UserMapper.cs                   # UserValueExt ↔ UserDto
│   ├── DoorMapper.cs                   # DoorValueExt ↔ DoorDto
│   ├── GroupMapper.cs                  # Group types ↔ GroupDto
│   ├── LogMapper.cs                    # LogValueExt ↔ LogEventDto
│   └── CommandMapper.cs                # DTO → CommandExt
├── Infrastructure/
│   ├── SessionManager.cs               # Abstracted session lifecycle
│   ├── WcfPaginationHelper.cs          # Generic pagination loop
│   ├── WcfRetryHandler.cs              # Retry logic for transient faults
│   └── BindingConfigFactory.cs         # Centralized NetTcpBinding builder
├── Helpers/
│   ├── SettingsHelper.cs               # KEPT for backward compat
│   └── PasswordHelper.cs               # Secure storage helpers
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  # Global exception → problem details
├── Settings/
│   └── Settings.json                   # KEPT as legacy override
├── Connected Services/
│   └── ACTServiceReference/            # UNCHANGED — auto-generated
└── Tests/
    ├── ACTApi.Tests.csproj
    ├── Integration/
    │   ├── UserServiceTests.cs
    │   ├── DoorServiceTests.cs
    │   └── PaginationTests.cs
    └── Mapper/
        ├── UserMapperTests.cs
        └── DoorMapperTests.cs
```

### Namespace Convention
```
ACTApi                         → Root (Program.cs, middleware)
ACTApi.Controllers             → HTTP controllers
ACTApi.Services                → Business logic / WCF wrappers
ACTApi.Models                  → Internal domain models
ACTApi.DTOs                    → API-facing data transfer objects
ACTApi.Mappers                 → WCF proxy type ↔ DTO conversions
ACTApi.Infrastructure          → Cross-cutting: session, pagination, retry, binding
ACTApi.Helpers                 → Legacy support utilities
ACTApi.Middleware              → ASP.NET middleware pipeline
```

---

## 2. Session Management Strategy

### Current Pattern (Keep)
```
Request → CreateProxy() → [operation] → CloseProxy()
```
- Scoped `IACTProServices` per HTTP request
- Short-lived sessions avoid WCF licence exhaustion
- `finally` block guarantees `CloseProxy()` even on exceptions

### Issues to Fix
1. **Missing MaxReceivedMessageSize override in custom binding**

   The current `CreateProxy()` creates a `NetTcpBinding` with default 64KB limit, but the generated default binding (`GetDefaultBinding`) correctly sets `MaxBufferSize = int.MaxValue`, `MaxReceivedMessageSize = int.MaxValue`, and `ReaderQuotas = XmlDictionaryReaderQuotas.Max`. The custom binding **must** match this, especially for operations returning large result sets (GetUsers, GetLogs, GetDoors).

2. **Connection string vs hostname confusion**

   `actServer` stores `"192.168.2.121:8004"` (host:port) but is used as `net.tcp://{actServer}/ActEnterprisePublicUintAPI` which produces `net.tcp://192.168.2.121:8004/ActEnterprisePublicUintAPI` — correct. The field name is misleading; rename to `actHostAndPort` or similar.

3. **Session state leaked across methods**

   Public fields `actServer`, `actUsername`, `actPassword`, `appName`, and `proxy` are all public and mutable. The proxy field in particular is a shared state hazard. Move to a dedicated session object.

### Recommended: `ActSession` Internal Model

```csharp
public class ActSession : IDisposable
{
    public ActEnterprisePublicAPI_ExtClient Proxy { get; }
    public uint SessionStatus { get; }
    public string Server { get; }
    // Dispose calls ShutDownSession + Close
}
```

Wrap creation in a `SessionManager`:

```csharp
public class SessionManager
{
    public async Task<ActSession> CreateSessionAsync();
    // Creates binding, endpoint, proxy, calls EstablishPublicSessionAsync
    // Returns ActSession or throws on failure
}
```

**Each domain service receives an `ActSession` and disposes it on completion.** This eliminates the shared-proxy anti-pattern and makes the lifecycle explicit.

---

## 3. Service Layer Design

### Principle: One Service Class Per Domain Aggregate

| Domain Service     | WCF Methods Used | Controller Consumers |
|--------------------|-----------------|---------------------|
| `UserService`      | GetUser, GetUsers, InsertUser, UpdateUser, DeleteUser, getBlankUserValue, GetUserWithCard | UsersController |
| `DoorService`      | GetDoors, GetDoorListing, IssueCommandOnDoors, IssueCommand, GetBlankCommand | AccessController, DoorsController |
| `GroupService`     | GetDoorGroups, GetUserGroups, Insert*, Update*, Delete* for all group types | GroupsController |
| `PhotoService`     | GetChunkSize, GetUserPhotoChunk, InsertUserPhotoChunk, ExportAllPhotos, ImportAllPhotos | PhotosController |
| `ExtraRightsService` | GetExtraRights, UpdateExtraRights, DeleteExtraRights, InsertExtraRights, GetBlankDoorPlanValue*, DoorPlan CRUD | ExtraRightsController |
| `MusterService`    | GetLogsOfUserTracking, GetListOfAbsentUsers, MusterReset | MusterController |
| `LogService`       | GetLogsOfEventType, GetLogsOfGlobalDoor, GetLogsOfUserID, GetMostRecentLogEvents, GetLogEvent, FindUser | LogsController |
| `ImportExportService` | ImportAllUsers, ExportAllUsers, ExportAllPhotos, ImportAllPhotos | ImportExportController |

### Service Interface Pattern

```csharp
public interface IUserService
{
    Task<UserDto> GetUserAsync(int userNumber);
    Task<PaginatedResponse<UserDto>> GetUsersAsync(UserSearchRequest request);
    Task<int> CreateUserAsync(UserDto user);
    Task UpdateUserAsync(UserDto user);
    Task<bool> DeleteUserAsync(int userNumber);
}
```

Each service:
1. Creates session via `SessionManager`
2. Performs WCF operation(s)
3. Maps WCF types → DTOs
4. Closes session
5. Returns clean API types

### Session Scope Decision

> **Option A: Per-method session** (Recommended for now)
> Each service method creates + destroys its own session. Simple, safe, no callback complexity. Higher latency per call (~50-200ms for establish/teardown) but acceptable for admin API calls.

> **Option B: Per-request session** (Future optimization)
> Create one session at controller action start, share across all service calls in that action, tear down at end. Requires careful exception handling and may run into WCF timeout if the action is long. Better for bulk operations.

Start with Option A. Profile later — if establish/teardown overhead is significant, switch to Option B with a session-per-scope middleware.

---

## 4. Controller Layer Design

### RESTful Conventions

| HTTP | Endpoint | Action |
|------|----------|--------|
| GET | `/api/users` | List users (paginated, filterable) |
| GET | `/api/users/{userNumber}` | Get single user |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{userNumber}` | Update user |
| DELETE | `/api/users/{userNumber}` | Delete user |
| GET | `/api/users/search?q=...` | Search users (alternative route) |
| GET | `/api/doors` | List doors (paginated) |
| GET | `/api/doors/{globalDoorNumber}` | Get single door |
| POST | `/api/doors/{globalDoorNumber}/access` | Allow access (existing) |
| POST | `/api/doors/{globalDoorNumber}/lock` | Lock door |
| POST | `/api/doors/{globalDoorNumber}/unlock` | Unlock door |
| POST | `/api/doors/{globalDoorNumber}/timed-unlock` | Momentary unlock |
| POST | `/api/doors/batch-command` | Command on multiple doors |
| GET | `/api/groups/door` | List door groups |
| POST | `/api/groups/door` | Create door group |
| GET | `/api/groups/door/{id}` | Get door group |
| PUT | `/api/groups/door/{id}` | Update door group |
| DELETE | `/api/groups/door/{id}` | Delete door group |
| GET | `/api/groups/user` | List user groups |
| POST | `/api/groups/user` | Create user group |
| GET | `/api/groups/user/{id}` | Get user group |
| PUT | `/api/groups/user/{id}` | Update user group |
| DELETE | `/api/groups/user/{id}` | Delete user group |
| GET | `/api/users/{userNumber}/extra-rights` | Get extra rights |
| PUT | `/api/users/{userNumber}/extra-rights` | Update extra rights |
| GET | `/api/users/{userNumber}/door-plan` | Get door plan |
| PUT | `/api/users/{userNumber}/door-plan` | Update door plan |
| GET | `/api/users/{userNumber}/photo` | Get user photo |
| PUT | `/api/users/{userNumber}/photo` | Set user photo |
| DELETE | `/api/users/{userNumber}/photo` | Delete user photo |
| GET | `/api/muster` | Muster report (who's on site) |
| GET | `/api/muster/absent` | Absent users report |
| POST | `/api/muster/reset` | Reset muster |
| GET | `/api/logs` | Query log events |
| GET | `/api/logs/user/{userNumber}` | Logs for specific user |
| GET | `/api/logs/door/{globalDoorNumber}` | Logs for specific door |
| POST | `/api/import/users` | CSV import |
| GET | `/api/export/users` | CSV export |

### Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers(
        [FromQuery] UserSearchRequest request)
    {
        var result = await _userService.GetUsersAsync(request);
        return Ok(result);
    }

    [HttpGet("{userNumber}")]
    public async Task<ActionResult<UserDto>> GetUser(int userNumber)
    {
        var user = await _userService.GetUserAsync(userNumber);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult> CreateUser(UserDto user)
    {
        var userNumber = await _userService.CreateUserAsync(user);
        return CreatedAtAction(nameof(GetUser), new { userNumber }, user);
    }
}
```

**Remove** `WeatherForecastController.cs` and `WeatherForecast.cs` — template boilerplate.  
**Remove** `ActApi.http` — superseded by Swagger UI.

---

## 5. Pagination Abstraction Pattern

### WCF Pagination Problem

The ACT WCF API uses start-index + max-count pagination with a 64KB message limit. Returned arrays are bounded; you must loop until the returned array is empty.

Example WCF signatures:
- `GetUsers(matchers, exactMatchers, start, finish, maxCount, order, enabled, enabledOption)` → `UserValueExt[]`
- `GetDoors(systemIndex, max, next, enabled)` → `DoorValueExt[]`
- `GetDoorGroups(index, max, next)` → `DoorGroupValue[]`

### Generic Paginator

```csharp
public static class WcfPaginationHelper
{
    /// <summary>
    /// Loops through a WCF paginated operation until empty, collecting all results.
    /// WCF page size is typically 100-200 (safe below 64KB).
    /// </summary>
    public static async Task<List<T>> GetAllAsync<T>(
        Func<int, int, Task<T[]>> fetchPage,
        int pageSize = 200)
    {
        var results = new List<T>();
        int startIndex = 0;
        T[] page;

        do
        {
            page = await fetchPage(startIndex, pageSize);
            if (page == null || page.Length == 0) break;
            results.AddRange(page);
            startIndex += page.Length;
        }
        while (page.Length == pageSize); // If less than pageSize, we're done

        return results;
    }
}
```

Usage:
```csharp
var allDoors = await WcfPaginationHelper.GetAllAsync<DoorValueExt>(
    (start, max) => proxy.GetDoorsAsync(start, max, true, true));
```

### API Surface Hides Pagination From Consumers

```csharp
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }      // If available from WCF (RowCount on BaseValue)
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
```

The API decides internally whether to:
- **Return a single page** (pass through): For quick lookups where 200 results are enough
- **Return all pages** (auto-loop): For exports or sync operations where completeness matters

Strategy: Start with **all-pages mode** (simple, right for admin UI), add **page-through mode** later as a query parameter:

```
GET /api/users?page=1&pageSize=200      # Explicit pagination
GET /api/users?pageSize=0                # All results (WCF loop internally)
```

---

## 6. Error Handling Strategy

### WCF Fault Taxonomy

| Fault Type | When | Strategy |
|-----------|------|----------|
| `FaultException` | ACT server returns error | Map to HTTP 400/409 with ACT error code |
| `CommunicationException` | Network failure, server down | Retry (up to 2x), then HTTP 503 |
| `TimeoutException` | 10s timeout exceeded | Retry once, then HTTP 504 |
| `InvalidOperationException` | Proxy not open | Force session recreation, retry |
| `EndpointNotFoundException` | ACT server unreachable | Immediate HTTP 503, no retry |
| `QuotaExceededException` | 64KB message limit | Reduce page size via configuration |

### Retry Handler

```csharp
public class WcfRetryHandler
{
    private readonly int _maxRetries = 2;

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        ILogger logger)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (TimeoutException) when (attempt < _maxRetries)
            {
                logger.LogWarning("WCF timeout on {Op}, retry {Attempt}/{Max}",
                    operationName, attempt, _maxRetries);
                await Task.Delay(500 * attempt);
            }
            catch (CommunicationException ex) when (attempt < _maxRetries)
            {
                logger.LogWarning("WCF comm error on {Op}: {Msg}, retry {Attempt}/{Max}",
                    operationName, ex.Message, attempt, _maxRetries);
                await Task.Delay(1000 * attempt);
            }
        }

        // Last attempt — let exception propagate
        return await operation();
    }
}
```

### Global Exception Middleware

```csharp
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FaultException ex)
        {
            context.Response.StatusCode = 400;
            await WriteProblemDetails(context, "ACT Server Error", ex.Message);
        }
        catch (CommunicationException ex) when (!ex.Data.Contains("exhausted"))
        {
            context.Response.StatusCode = 503;
            await WriteProblemDetails(context, "ACT Server Unreachable", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await WriteProblemDetails(context, "Internal Server Error", null);
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext ctx, string title, string detail)
    {
        var response = new
        {
            Type = "https://tools.ietf.org/html/rfc7231",
            Title = title,
            Status = ctx.Response.StatusCode,
            Detail = detail ?? "An unexpected error occurred.",
            TraceId = Activity.Current?.Id ?? ctx.TraceIdentifier
        };
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(response);
    }
}
```

### Session Timeout Recovery

Current CreateProxy uses 10s timeouts. If the ACT server has been idle (e.g., overnight), the first session establishment may fail. Recommendation:
- **OpenTimeout**: 15s (generous for first connect)
- **SendTimeout**: 30s (for bulk user inserts)
- **ReceiveTimeout**: 30s (for large result sets)
- **CloseTimeout**: 10s

For photo chunk operations, increase SendTimeout to 60s since chunks may be large.

---

## 7. DTO Mapping Strategy

### Principle: Never Expose WCF Proxy Types in API Responses

The generated types (`UserValueExt`, `DoorValueExt`, etc.) are:
- Bloated (many internal fields like `ReadTimeField`, `ErrorField`, `RowCountField`)
- UI-specific (descriptor fields like `CardTypeDescriptor`)
- Coupled to WCF serialization attributes
- Potentially breaking if the WCF contract changes

### Mapping Approach: Hand-rolled Extension Methods

Auto-mappers (AutoMapper, Mapster) add complexity for little benefit here — the WCF types have many fields with slightly different names. Hand-rolled mappers give full control and are trivially testable.

```csharp
// DTOs/UserDto.cs
public class UserDto
{
    public int UserNumber { get; set; }
    public string Forename { get; set; }
    public string Surname { get; set; }
    public int Group { get; set; }
    public string GroupName { get; set; }
    public int Pin { get; set; }
    public bool Enabled { get; set; }
    public string CardType { get; set; }       // Resolved from uint via enum
    public List<string> UserFields { get; set; }
    public DateTime? EndValid { get; set; }
    public bool HasPhoto { get; set; }
    public DateTime? RecordCreated { get; set; }
    // Only expose fields the REST consumer needs
}

// Mappers/UserMapper.cs
public static class UserMapper
{
    public static UserDto ToDto(this UserValueExt source)
    {
        return new UserDto
        {
            UserNumber = source.UserNumber,
            Forename = source.Forename,
            Surname = source.Surname,
            Group = source.Group,
            GroupName = source.GroupName,
            Pin = source.Pin,
            Enabled = source.Enabled,
            // Resolve uint to description:
            CardType = source.CardType switch
            {
                0 => "Unknown",
                1 => "Learned",
                2 => "OneToOne",
                4 => "SiteCoded1",
                8 => "SiteCoded2",
                _ => $"Other({source.CardType})"
            },
            UserFields = source.UserFields?.ToList() ?? new(),
            EndValid = source.EndValid == DateTime.MinValue ? null : source.EndValid,
            HasPhoto = source.HasPhotograph,
            RecordCreated = source.RecordCreated == DateTime.MinValue
                ? null : source.RecordCreated
        };
    }

    public static UserValueExt ToWcf(this UserDto source)
    {
        return new UserValueExt
        {
            UserNumber = source.UserNumber,
            Forename = source.Forename ?? "",
            Surname = source.Surname ?? "",
            Group = source.Group,
            Pin = source.Pin,
            Enabled = source.Enabled,
            IsValid = true,                // REQUIRED for insert/update
            ExternallyModified = 0,        // DoNothing
            UserFields = source.UserFields?.ToArray() ?? new string[10],
            // Default values for WCF-required fields:
            Cards = new uint[5],
            CardEditable = new bool[5],
        };
    }
}
```

### DTOs for Pagination & Search

```csharp
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalResults { get; set; }
    public bool HasMore { get; set; }
}

public class UserSearchRequest
{
    public string? Forename { get; set; }
    public string? Surname { get; set; }
    public int? Group { get; set; }
    public uint? CardNumber { get; set; }
    public string? UserField { get; set; }
    public int? UserFieldIndex { get; set; }        // 0-9
    public bool? Enabled { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 200;
}
```

### Key Mapping Decisions

| WCF Field | DTO Decision | Rationale |
|-----------|-------------|-----------|
| `UserValueExt.Cards[0..4]` | Expose as `List<uint>` only if needed | Most consumers use Card1 only |
| `UserValueExt.CardType` (uint) | Resolve to string descriptor | Enum bitfield; consumers want "SiteCoded1" not "4" |
| `UserValueExt.ExternallyModified` | Always map to 0 on insert/update | WCF spec says DoNothing=0 |
| `UserValueExt.IsValid` | Always true on insert | WCF rejects invalid records |
| `BaseValue.RowCount` | Map to `PaginationInfo.TotalResults` | Useful metadata from WCF |
| `BaseValue.Error` | Log server-side, don't expose to API caller | ACT error messages aren't consumer-friendly |
| `CommandExt.Type` (uint) | Resolve to enum name via DTO property | "Door", "Device", "IOModule" |

---

## 8. Settings & Secrets Management

### Current Problems

1. **ACT password stored in plaintext** in `Settings/Settings.json`
2. **Hardcoded JSON path** — `Path.Combine(exeDirectory, @"Settings\Settings.json")`
3. **SettingsHelper is a static/singleton hybrid** — static `JObject` + instance properties
4. **Newtonsoft.Json dependency** — unnecessary when `System.Text.Json` is available in .NET 8
5. **No validation** — malformed settings crash at runtime with no helpful message

### Recommended Approach

**Option A: Merge into `appsettings.json`** (Recommended — simplest)

```json
{
  "Logging": { ... },
  "AllowedHosts": "*",
  "ActSettings": {
    "Server": "http://localhost:8021",
    "ActHost": "192.168.2.121",
    "ActPort": 8004,
    "ActUsername": "fenky",
    "ActPassword": "passwordsucks",
    "AppName": "RVMS_ACT_Plugin"
  }
}
```

Use the Options pattern:
```csharp
public class ActSettings
{
    public string Server { get; set; } = "http://localhost:8021";
    public string ActHost { get; set; } = "192.168.2.121";
    public int ActPort { get; set; } = 8004;
    public string ActUsername { get; set; } = "";
    public string ActPassword { get; set; } = "";
    public string AppName { get; set; } = "RVMS_ACT_Plugin";

    public string ActEndpoint => $"net.tcp://{ActHost}:{ActPort}/ActEnterprisePublicUintAPI";
}
```

Registration in `Program.cs`:
```csharp
builder.Services.Configure<ActSettings>(
    builder.Configuration.GetSection("ActSettings"));
```

**Option B: Keep `Settings/Settings.json` but add validation + encryption**  
If the InnoSetup installer's post-install script depends on the Settings.json file format, keep it. Add:
- `Data Protection API (DPAPI)` encryption for `ACTPassword`
- Validation on startup
- `IOptionsSnapshot` reloading for runtime changes

**Recommendation: Do Option A**, then update the InnoSetup script (which uses a Pascal Script `UpdateSettingsFile` procedure) to write to the appropriate `appsettings.json` section instead. The installer already has hook points (`CurStepChanged` → `ssPostInstall`).

---

## 9. Testing Strategy

### Unit Testing
| Target | Framework | What to Test |
|--------|-----------|-------------|
| Mappers | xUnit + FluentAssertions | WCF type → DTO mapping, null handling, edge cases (min/max dates, empty arrays) |
| WcfPaginationHelper | xUnit + Moq | Looping logic, boundary conditions (empty page, partial last page) |
| WcfRetryHandler | xUnit + Moq | Retry count, exception type routing, backoff delay |
| DTO validation | xUnit | Required fields, string lengths, PIN range (0-999999) |
| CommandMapper | xUnit | DTO door command → CommandExt mapping, enum resolution |

### Integration Testing
| Target | What to Test | Notes |
|--------|-------------|-------|
| SessionManager | Establish + teardown lifecycle | Requires running ACT server (192.168.2.121) |
| UserService | Full CRUD round-trip | Insert user → read-back → update → delete |
| DoorService | List doors + issue command | At least one door must be configured |
| PhotoService | Chunked get/set round-trip | Photo must be under chunk size limit |

### Recommended Setup

```xml
<!-- ACTApi.Tests.csproj -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.*" /> <!-- For integration tests -->
```

**Integration test approach:** Create a `TestStartup` that uses a mock `SessionManager` or targets a test/CI ACT server. Use `WebApplicationFactory<Program>` to spin up the full ASP.NET pipeline with an in-memory test server.

---

## 10. Deployment Considerations

### Windows Service (Already Working)

| Aspect | Current | Assessment |
|--------|---------|------------|
| `UseWindowsService()` | ✅ Already wired | Correct approach |
| Service name | `ACTApiService` (in .iss) | Consistent |
| Startup type | Auto (set by installer) | ✅ |
| Recovery options | 3 restarts with 60s delay | ✅ (set in .iss) |
| Account | Configurable in installer (`.\\Administrator` or domain account) | ✅ |

### InnoSetup Installer (`ACTProAPISetup.iss`)

Current installer is mature (311 lines) with:
- Server config wizard page (HTTP host/port + ACT host/port)
- ACT credentials wizard page
- Service account wizard page with validation
- Firewall rules for HTTP and ACT ports
- icacls permissions for Logs and Settings folders
- Service create/start/stop/delete lifecycle

**Minor improvements needed:**
1. Update `UpdateSettingsFile` to write `appsettings.json` instead of `Settings.json` (if migrating to Options pattern)
2. Add a health check URL test after installation (e.g., `curl http://localhost:8021/swagger`)
3. The `PrivilegesRequired=admin` setting is correct — service installation requires admin

### Firewall Configuration

Current installer creates two inbound rules:
- TCP on HTTP port (e.g., 8021) — for REST API consumers
- TCP on ACT port (e.g., 8004) — for WCF outbound to ACT server

**Note:** The ACT outbound rule (`ACTApi ACT` on localport 8004) only makes sense if the API service binds to that port locally (it doesn't — it's an outbound client). This rule should be removed or changed to allow outbound connections on that port. Likely harmless in practice but misleading.

### Resource Requirements

| Resource | Estimate | Notes |
|----------|---------|-------|
| RAM | 50-100 MB idle, 200-500 MB under load | WCF proxy + serialization buffers + page data |
| Disk | 50 MB for binaries, ~10 MB/day logs (rolling) | Serilog rolling daily |
| Network | Minimal — one TCP connection per request | Session establish/teardown per call |
| ACT Licences | 1 per concurrent request | Scoped services, so N concurrent HTTP requests = N ACT licences |

### Monitoring & Observability

| Need | Solution | Notes |
|------|---------|-------|
| Health check | Add `GET /health` endpoint | Returns 200 if ACT server is reachable |
| Structured logging | Already have Serilog | Add `WriteTo.Seq()` for centralized log viewing |
| Metrics | Add `dotnet-counters` or OpenTelemetry | Track session establish times, request durations, error rates |
| Memory dumps | Configure `DOTNET_DbgMiniDumpType=2` | For production crash analysis |

---

## Summary of Action Items (Priority Order)

### Critical (Must Fix Before Expansion)
1. **Fix MaxReceivedMessageSize in `ACTProServices.CreateProxy()`** — Add `MaxBufferSize = int.MaxValue`, `MaxReceivedMessageSize = int.MaxValue`, and `ReaderQuotas.Max` to the custom `NetTcpBinding`. Without this, any operation returning >64KB will silently fail.
2. **Fix public mutable fields** — `proxy`, `actServer`, etc. should be private. Encapsulate session state in an `ActSession` disposable class.

### High (Core Expansion)
3. **Remove boilerplate** — Delete `WeatherForecastController.cs`, `WeatherForecast.cs`, `ACTApi.http`
4. **Create DTOs** — `UserDto`, `DoorDto`, `PaginatedResponse<T>`, `DoorCommandRequest`, etc.
5. **Create mappers** — Extension methods for WCF type → DTO conversion
6. **Create `WcfPaginationHelper`** — Generic pagination loop for all list operations
7. **Refactor `ACTProServices`** into domain services — `UserService`, `DoorService`, etc.
8. **Add `ExceptionHandlingMiddleware`** — Global fault → HTTP status mapping

### Medium (Quality & Security)
9. **Migrate Settings to Options pattern** — Use `appsettings.json` + `IOptions<ActSettings>`
10. **Create `WcfRetryHandler`** — Retry logic for transient WCF faults
11. **Add integration tests** — At minimum, session lifecycle, user CRUD, door commands

### Low (Nice-to-Have)
12. **Update InnoSetup installer** — Post-install health check, fix misleading firewall rule
13. **Add health check endpoint** — `GET /health` with ACT server ping
14. **Secure password storage** — DPAPI encryption or Windows Credential Manager
15. **OpenTelemetry metrics** — Request duration histogram, session count gauge
