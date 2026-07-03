using ACTApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTApi.Controllers
{
    /// <summary>
    /// Provides access control operations (granting door access) against
    /// the ACT Enterprise WCF API.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AccessController : ControllerBase
    {
        private readonly ILogger<AccessController> _logger;
        private readonly IACTProServices _actProServices;

        /// <summary>Initializes a new instance of <see cref="AccessController"/>.</summary>
        public AccessController(
            IACTProServices actProServices,
            ILogger<AccessController> logger)
        {
            _logger = logger;
            _actProServices = actProServices;
        }

        /// <summary>Grants access by activating a relay on the specified door.</summary>
        /// <param name="globalDoorNumber">
        /// The ACT global door number to activate.</param>
        /// <response code="200">
        /// Access granted successfully.</response>
        /// <response code="400">
        /// Invalid door number or proxy not initialized.</response>
        /// <response code="503">
        /// ACT server unreachable or session failed.</response>
        [HttpPost("allowaccess")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> AllowAccess(int globalDoorNumber)
        {
            await _actProServices.CreateProxy();
            try
            {
                await _actProServices.AllowAccess(globalDoorNumber);
                return Ok($"Access granted for door number {globalDoorNumber}");
            }
            finally
            {
                await _actProServices.CloseProxy();
            }
        }
    }
}
