using ACTApi.Middleware;
using ACTApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace ACTApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ── Bootstrap Logger (catches startup errors) ──────────────
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.ServiceModel", LogEventLevel.Warning)
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "actapi-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {MachineName} [{ThreadId}] {Message:lj}{NewLine}{Exception}")
                .CreateBootstrapLogger();

            try
            {
                // ── Load Runtime Settings ──────────────────────────────
                var settings = new Helpers.SettingsHelper();

                Log.Information("ACT API Bridge starting up...");
                Log.Information("ACT Server target: {ActServer}, App: {AppName}",
                    settings.actServer, settings.appName);
                Log.Information("HTTP endpoint: {HttpUrl}", settings.serverAddress);
                Log.Information("Runtime: {IsService}, BaseDir: {BaseDir}",
                    OperatingSystem.IsWindows(), AppDomain.CurrentDomain.BaseDirectory);

                var builder = WebApplication.CreateBuilder(args);

                // Load appsettings.json if it exists (optional — defaults used otherwise)
                var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(appSettingsPath))
                {
                    builder.Configuration.AddJsonFile(appSettingsPath, optional: true, reloadOnChange: true);
                }

                // Load ACT-specific settings
                builder.Configuration.AddJsonFile(settings.sharedSettingsPath,
                    optional: false, reloadOnChange: true);

                // ── Windows Service Hosting (safe during dev; only activates when installed) ─
                if (OperatingSystem.IsWindows())
                {
                    builder.Host.UseWindowsService();
                }
                else
                {
                    Log.Information("Not running on Windows — skipping Windows Service registration");
                }

                builder.WebHost.UseUrls(settings.serverAddress);

                // ── Serilog for the application pipeline ─────────────────
                builder.Host.UseSerilog((context, services, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                        .MinimumLevel.Override("System.ServiceModel", LogEventLevel.Warning)
                        .Enrich.WithMachineName()
                        .Enrich.WithProcessId()
                        .Enrich.WithThreadId()
                        .WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File(
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "actapi-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {MachineName} [{ThreadId}] {Message:lj}{NewLine}{Exception}");
                });

                // ── Register Services ───────────────────────────────────
                builder.Services.AddSingleton<Helpers.SettingsHelper>();
                builder.Services.AddScoped<IACTProServices, ACTProServices>();
                builder.Services.AddScoped<IUserService, UserService>();
                builder.Services.AddScoped<IDoorService, DoorService>();
                builder.Services.AddScoped<IGroupService, GroupService>();
                builder.Services.AddScoped<IPhotoService, PhotoService>();
                builder.Services.AddScoped<IExtraRightsService, ExtraRightsService>();
                builder.Services.AddScoped<IMusterService, MusterService>();
                builder.Services.AddScoped<ILogService, LogService>();
                builder.Services.AddScoped<IImportExportService, ImportExportService>();

                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();

                // ── Swagger (read from config; default on for admin API) ──
                var swaggerEnabled = builder.Configuration
                    .GetValue<bool>("SwaggerEnabled", true);
                if (swaggerEnabled)
                {
                    builder.Services.AddSwaggerGen(options =>
                    {
                        options.SwaggerDoc("v1", new OpenApiInfo
                        {
                            Title = "ACT API Bridge",
                            Version = "v1.0.0",
                            Description = "RESTful HTTP bridge for ACT Enterprise access control WCF API. " +
                                          "Provides CRUD operations for users, doors, groups, muster, " +
                                          "and real-time door commands.",
                            Contact = new OpenApiContact
                            {
                                Name = "Entech Security",
                                Url = new Uri("https://entechsecurity.com")
                            }
                        });

                        // Include XML comments if XML doc file is generated
                        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                        if (File.Exists(xmlPath))
                        {
                            options.IncludeXmlComments(xmlPath);
                        }
                    });
                }

                var app = builder.Build();

                // ── Middleware Pipeline ──────────────────────────────────
                app.UseMiddleware<ExceptionHandlingMiddleware>();
                app.UseMiddleware<RequestLoggingMiddleware>();

                if (swaggerEnabled)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(options =>
                    {
                        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ACT API Bridge v1");
                        options.RoutePrefix = "swagger";
                    });
                }

                app.UseAuthorization();
                app.MapControllers();

                Log.Information("ACT API Bridge is ready — listening on {HttpUrl}", settings.serverAddress);
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "ACT API Bridge terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
