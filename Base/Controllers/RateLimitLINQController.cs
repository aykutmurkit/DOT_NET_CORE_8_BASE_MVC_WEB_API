using Base.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Base.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RateLimitLINQController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RateLimitLINQController> _logger;
        private readonly RateLimitingService _rateLimitingService;

        public RateLimitLINQController(
            IConfiguration configuration,
            ILogger<RateLimitLINQController> logger,
            RateLimitingService rateLimitingService)
        {
            _configuration = configuration;
            _logger = logger;
            _rateLimitingService = rateLimitingService;
        }

        [HttpGet("test")]
        public ActionResult<ApiResponse<object>> TestRateLimit()
        {
            return ApiResponse<object>.Success(
                new { message = "Rate limit test endpoint. Call this multiple times to see rate limiting in action." }, 
                "Test rate limiting endpoint called successfully."
            );
        }

        [HttpGet("fast")]
        public ActionResult<ApiResponse<object>> FastRateLimit()
        {
            return ApiResponse<object>.Success(
                new { message = "Fast rate limit test endpoint. This endpoint has a stricter rate limit than others." },
                "Fast rate limiting endpoint called successfully."
            );
        }

        [HttpGet("config")]
        public ActionResult<ApiResponse<object>> GetRateLimitConfig()
        {
            var generalRules = _rateLimitingService.GetGeneralRules();
            var endpointRules = _rateLimitingService.GetEndpointSpecificRules();
            var ipWhitelist = _rateLimitingService.GetIpWhitelist();
            var endpointWhitelist = _rateLimitingService.GetEndpointWhitelist();

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var isWhitelisted = ipWhitelist.Contains(clientIp) || 
                                ipWhitelist.Any(range => clientIp.StartsWith(range.TrimEnd('*')));

            return ApiResponse<object>.Success(
                new
                {
                    YourIp = clientIp,
                    IsWhitelisted = isWhitelisted,
                    GeneralRules = generalRules,
                    EndpointSpecificRules = endpointRules,
                    IpWhitelist = ipWhitelist,
                    EndpointWhitelist = endpointWhitelist
                },
                "Rate limit configuration retrieved successfully."
            );
        }

        [HttpGet("info")]
        public ActionResult<ApiResponse<object>> RateLimitInfo()
        {
            return ApiResponse<object>.Success(
                new
                {
                    HowToTest = "Call /api/RateLimitLINQ/test or /api/RateLimitLINQ/fast multiple times to trigger rate limiting",
                    ConfigurationInfo = "Rate limits are configured in appsettings.json under RateLimitOptions and IpRateLimiting sections",
                    EndpointFormat = "Endpoint format is '{METHOD}:{PATH}', e.g. 'GET:/api/RateLimitLINQ/test'",
                    SampleCustomRule = new { Endpoint = "GET:/api/RateLimitLINQ/fast", Period = "10s", Limit = 3 },
                    PeriodFormats = new 
                    { 
                        Seconds = "1s, 5s, 10s, etc.", 
                        Minutes = "1m, 5m, 15m, etc.",
                        Hours = "1h, 6h, 12h, etc.",
                        Days = "1d, 7d, 30d, etc." 
                    }
                },
                "Rate limit usage information retrieved successfully."
            );
        }
    }
} 