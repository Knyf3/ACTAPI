using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// Proxies Google Drive thumbnail images so they work from HTTP/local origins.
/// Google Drive's thumbnail endpoint blocks HTTP referrers — this endpoint
/// fetches the image server-side (no referrer restrictions) and serves it
/// same-origin to the requesting page.
/// </summary>
[ApiController]
[Route("api/photo-proxy")]
[Produces("application/json")]
public class PhotoProxyController : ControllerBase
{
    private readonly ILogger<PhotoProxyController> _logger;
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public PhotoProxyController(ILogger<PhotoProxyController> logger)
    {
        _logger = logger;
    }

    /// <summary>Proxies a Google Drive file thumbnail by file ID.</summary>
    /// <param name="id">The Google Drive file ID (alphanumeric, underscores, hyphens only).</param>
    [HttpGet]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ProxyPhoto([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { Message = "File ID is required." });

        // Security: only allow alphanumeric, underscore, and hyphen
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-zA-Z0-9_-]+$"))
            return BadRequest(new { Message = "Invalid file ID format." });

        var thumbnailUrl = $"https://drive.google.com/thumbnail?id={id}&sz=w800";

        try
        {
            var response = await _httpClient.GetAsync(thumbnailUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Drive returned {StatusCode} for thumbnail {FileId}",
                    response.StatusCode, id);
                return StatusCode(502, new { Message = $"Google Drive returned {response.StatusCode}." });
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";

            // Cache for 1 hour — these thumbnails don't change
            Response.Headers.Append("Cache-Control", "public, max-age=3600");

            return File(imageBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to proxy photo {FileId}", id);
            return StatusCode(502, new { Message = "Failed to fetch photo from Google Drive." });
        }
    }
}
