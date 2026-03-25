using ACTProAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ACTProAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccessController : Controller
    {
        private readonly ILogger<AccessController> _logger;
        private readonly IACTProServices _actProServices;

        public AccessController(IACTProServices actProServices, ILogger<AccessController> logger)
        {
            _logger = logger;
            _actProServices = actProServices;
        }

        [HttpPost("allowaccess")]
        public async Task<IActionResult> AllowAccess(int globalDoorNumber)
        {
            try
            {
                await _actProServices.CreateProxy();
                await _actProServices.AllowAccess(globalDoorNumber);
                return Ok($"Access granted for door number {globalDoorNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting access");
                return StatusCode(500, "An error occurred while granting access");
            }
            finally
            {
                await _actProServices.CloseProxy();
            }
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}
    }
}
