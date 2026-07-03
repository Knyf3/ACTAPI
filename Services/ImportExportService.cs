using System.Globalization;
using System.Text;
using ACTApi.Infrastructure;
using ACTApi.Mappers;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Domain service for ACT import/export operations.</summary>
public class ImportExportService : IImportExportService
{
    private readonly ILogger<ImportExportService> _logger;

    /// <summary>Initializes a new instance of <see cref="ImportExportService"/>.</summary>
    public ImportExportService(ILogger<ImportExportService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> ImportUsersAsync(ActEnterprisePublicAPI_ExtClient proxy, Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        int count = 0;

        // Skip header line
        var headerLine = await reader.ReadLineAsync();

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = line.Split(',');
            if (fields.Length < 2)
                continue;

            var forename = fields[0].Trim('"');
            var surname = fields[1].Trim('"');

            var userDto = new DTOs.UserDto
            {
                Forename = forename,
                Surname = surname,
                Enabled = true,
                Group = fields.Length > 2 && int.TryParse(fields[2].Trim(), out var g) ? g : 1,
                Pin = fields.Length > 3 && int.TryParse(fields[3].Trim(), out var p) ? p : 0
            };

            var wcfUser = userDto.ToWcf();
            await WcfCallLogger.ExecuteAsync(
                () => proxy.InsertUserAsync(wcfUser, true),
                "ImportUser",
                _logger);

            count++;
        }

        _logger.LogInformation("Imported {Count} users from CSV", count);
        return count;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportUsersAsync(ActEnterprisePublicAPI_ExtClient proxy)
    {
        var users = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetUsersAsync(
                new Dictionary<string, string>(),
                new Dictionary<string, int>(),
                0, 0, 10000, true, false, 0),
            "GetUsers",
            _logger);

        var sb = new StringBuilder();
        sb.AppendLine("Forename,Surname,Group,PIN,CardNumber,Enabled");

        if (users != null)
        {
            foreach (var user in users)
            {
                var cards = new List<uint>();
                if (user.LearnedCard != 0) cards.Add(user.LearnedCard);
                if (user.SiteCodedCard != 0) cards.Add(user.SiteCodedCard);
                if (user.OneToOneCard != 0) cards.Add(user.OneToOneCard);

                var cardStr = cards.Count > 0 ? cards[0].ToString() : "";

                sb.AppendLine(
                    $"\"{EscapeCsv(user.Forename)}\",\"{EscapeCsv(user.Surname)}\",{user.Group},{user.Pin},{cardStr},{user.Enabled}");
            }
        }

        _logger.LogInformation("Exported {Count} users to CSV", users?.Length ?? 0);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return value.Replace("\"", "\"\"");

        return value;
    }
}
