using ACTApi.Middleware;
using ACTApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace ACTApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // ── Load Settings (single source of truth: Settings/Settings.json) ──
            var settings = new Helpers.SettingsHelper();

            // ── Bootstrap Logger ────────────────────────────────────────
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
                Log.Information("ACT API Bridge starting up...");
                Log.Information("Settings file: {SettingsPath}", settings.sharedSettingsPath);
                Log.Information("ACT Server: {ActServer}, App: {AppName}",
                    settings.actServer, settings.appName);
                Log.Information("HTTP endpoint: {HttpUrl}", settings.serverAddress);

                var builder = WebApplication.CreateBuilder(args);

                // ── Single Configuration Source: Settings.json ─────────────
                // This file includes Server, ACTServer, credentials, SwaggerEnabled,
                // Logging levels, and Serilog config — everything in one place.
                builder.Configuration.AddJsonFile(settings.sharedSettingsPath,
                    optional: false, reloadOnChange: true);

                // ── Windows Service Hosting ─────────────────────────────
                if (OperatingSystem.IsWindows())
                {
                    builder.Host.UseWindowsService();
                }

                builder.WebHost.UseUrls(settings.serverAddress);

                // ── Serilog ─────────────────────────────────────────────
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

                // ── CORS (from Settings.json: CorsOrigins) ─────────────────
                if (!string.IsNullOrWhiteSpace(settings.corsOrigins))
                {
                    builder.Services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(policy =>
                        {
                            policy.WithOrigins(settings.corsOrigins.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                                  .WithHeaders("Content-Type");
                        });
                    });
                }

                // ── Swagger (from Settings.json: SwaggerEnabled) ────────
                if (settings.swaggerEnabled)
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

                if (settings.swaggerEnabled)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(options =>
                    {
                        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ACT API Bridge v1");
                        options.RoutePrefix = "swagger";
                    });
                }

                // ── Static Files (LITEVM Verify Page) ─────────────────────────────
                if (settings.verifyPageEnabled)
                {
                    var verifyBasePath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "verify");

                    if (Directory.Exists(verifyBasePath))
                    {
                        var verifyFileProvider = new PhysicalFileProvider(verifyBasePath);

                        // Default files → /verify/ serves index.html
                        app.UseDefaultFiles(new DefaultFilesOptions
                        {
                            FileProvider = verifyFileProvider,
                            RequestPath = "/verify",
                            DefaultFileNames = new[] { "index.html" }
                        });

                        // Serve static files under /verify/ path
                        app.UseStaticFiles(new StaticFileOptions
                        {
                            FileProvider = verifyFileProvider,
                            RequestPath = "/verify",
                            OnPrepareResponse = ctx =>
                            {
                                ctx.Context.Response.Headers.Append(
                                    "Cache-Control", "public, max-age=3600");
                                ctx.Context.Response.Headers.Append(
                                    "X-Frame-Options", "SAMEORIGIN");
                            }
                        });

                        Log.Information(
                            "Verify page enabled — serving from {Path} at {Url}/verify/",
                            verifyBasePath, settings.serverAddress);
                    }
                    else
                    {
                        Log.Warning(
                            "Verify page is enabled but directory not found: {Path}",
                            verifyBasePath);
                    }
                }

                app.UseCors();
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
