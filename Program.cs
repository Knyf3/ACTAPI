using Microsoft.OpenApi.Models;
using Serilog;

namespace ACTProAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var settings = new Helpers.SettingsHelper();
            string httpUrl = settings.serverAddress;

            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile(settings.sharedSettingsPath, optional: false, reloadOnChange: true);

            //Create as a windows service
            builder.Host.UseWindowsService();
            builder.WebHost.UseUrls(httpUrl);

            // Use Serilog as the logging provider
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var logPath = Path.Combine(exeDirectory, "Logs", "log-.txt");
                configuration
                    .WriteTo.Console()
                    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
            });

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "RVMS Service",
                    Version = "v1"
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
