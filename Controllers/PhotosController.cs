using ACTApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers;

/// <summary>
/// User photo streaming endpoints (JPEG chunked read/write via ACT WCF).
/// </summary>
[ApiController]
[Route("api/users/{userNumber}")]
[Produces("application/json")]
public class PhotosController : ControllerBase
{
    private readonly ILogger<PhotosController> _logger;
    private readonly IACTProServices _actProServices;
    private readonly IPhotoService _photoService;

    /// <summary>Initializes a new instance of <see cref="PhotosController"/>.</summary>
    public PhotosController(
        ILogger<PhotosController> logger,
        IACTProServices actProServices,
        IPhotoService photoService)
    {
        _logger = logger;
        _actProServices = actProServices;
        _photoService = photoService;
    }

    /// <summary>Gets a user's photo as a JPEG image.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    [HttpGet("photo")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetPhoto(int userNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var photoData = await _photoService.GetUserPhotoAsync(_actProServices.CurrentProxy!, userNumber);
            if (photoData == null || photoData.Length == 0)
                return NotFound(new { Message = $"No photo found for user {userNumber}." });

            return File(photoData, "image/jpeg", $"user_{userNumber}.jpg");
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Uploads a photo (JPEG) for a user.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    /// <param name="file">JPEG file to upload.</param>
    [HttpPut("photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SetPhoto(int userNumber, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "No file provided or file is empty." });

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Message = "Only image files are accepted." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var photoData = ms.ToArray();

        await _actProServices.CreateProxy();
        try
        {
            var result = await _photoService.SetUserPhotoAsync(_actProServices.CurrentProxy!, userNumber, photoData);
            return Ok(new { Message = $"Photo for user {userNumber} uploaded ({photoData.Length} bytes).", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }

    /// <summary>Deletes a user's photo.</summary>
    /// <param name="userNumber">The ACT user number.</param>
    [HttpDelete("photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeletePhoto(int userNumber)
    {
        await _actProServices.CreateProxy();
        try
        {
            var result = await _photoService.DeleteUserPhotoAsync(_actProServices.CurrentProxy!, userNumber);
            return Ok(new { Message = $"Photo for user {userNumber} deleted.", Success = result });
        }
        finally
        {
            await _actProServices.CloseProxy();
        }
    }
}
