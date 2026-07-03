using ACTApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// Import and export endpoints for user data via CSV.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ImportExportController : ControllerBase
{
    private readonly ILogger<ImportExportController> _logger;
    private readonly IACTProServices _actProServices;
    private readonly IImportExportService _importExportService;

    /// <summary>Initializes a new instance of <see cref="ImportExportController"/>.</summary>
    public ImportExportController(
        ILogger<ImportExportController> logger,
        IACTProServices actProServices,
        IImportExportService importExportService)
    {
        _logger = logger;
        _actProServices = actProServices;
        _importExportService = importExportService;
    }

    /// <summary>Imports users from a CSV file upload.</summary>
    /// <param name="file">CSV file with columns: Forename,Surname,Group,PIN.</param>
    [HttpPost("import/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ImportUsers(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "No file provided or file is empty." });

        await _actProServices.CreateProxy();
        try
        {
            using var stream = file.OpenReadStream();
            var count = await _importExportService.ImportUsersAsync(_actProServices.CurrentProxy!, stream);
            return Ok(new { Message = $"Imported {count} users.", Count = count });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Exports all users to a CSV file download.</summary>
    [HttpGet("export/users")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ExportUsers()
    {
        await _actProServices.CreateProxy();
        try
        {
            var csvData = await _importExportService.ExportUsersAsync(_actProServices.CurrentProxy!);
            return File(csvData, "text/csv", $"act_users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }
}
