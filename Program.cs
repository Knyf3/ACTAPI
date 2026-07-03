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
            // Load settings early for log path resolution
            var settings = new Helpers.SettingsHelper();

            // ── Serilog Bootstrap ────────────────────────────────────────
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile(settings.sharedSettingsPath,
                        optional: false, reloadOnChange: true)
                    .Build())
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
                    retainedFileCountLimit: 30,       // Keep 30 days of logs
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {MachineName} [{ThreadId}] {Message:lj}{NewLine}{Exception}")
                .CreateBootstrapLogger();

            try
            {
                Log.Information("ACT API Bridge starting up...");
                Log.Information("Settings loaded from: {SettingsPath}", settings.sharedSettingsPath);
                Log.Information("ACT Server target: {ActServer}, App: {AppName}",
                    settings.actServer, settings.appName);
                Log.Information("HTTP endpoint: {HttpUrl}", settings.serverAddress);

                var builder = WebApplication.CreateBuilder(args);

                builder.Configuration.AddJsonFile(settings.sharedSettingsPath,
                    optional: false, reloadOnChange: true);

                // ── Windows Service Hosting ──────────────────────────────
                builder.Host.UseWindowsService();
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
                // 1. Exception handling first — catches errors from everything below
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                // 2. Request logging — captures method, path, status, duration
                app.UseMiddleware<RequestLoggingMiddleware>();

                // 3. Swagger (before routing)
                if (swaggerEnabled)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(options =>
                    {
                        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ACT API Bridge v1");
                        options.RoutePrefix = "swagger";
                    });
                }

                // 4. Routing
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
