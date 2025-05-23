using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

namespace Base.Utilities
{
    public class RateLimitingService
    {
        private readonly IpRateLimitOptions _ipRateLimitOptions;
        private readonly RateLimitOptions _rateLimitOptions;
        private readonly IConfiguration _configuration;

        public RateLimitingService(
            IOptions<IpRateLimitOptions> ipRateLimitOptions,
            IConfiguration configuration)
        {
            _ipRateLimitOptions = ipRateLimitOptions.Value;
            _configuration = configuration;
            _rateLimitOptions = _configuration.GetSection("RateLimitOptions").Get<RateLimitOptions>() ?? new RateLimitOptions();
        }

        public IEnumerable<RateLimitRule> GetGeneralRules()
        {
            return _rateLimitOptions.GeneralRules;
        }

        public IEnumerable<RateLimitRule> GetEndpointSpecificRules()
        {
            return _rateLimitOptions.EndpointSpecificRules;
        }

        public IEnumerable<string> GetIpWhitelist()
        {
            return _ipRateLimitOptions.IpWhitelist;
        }

        public IEnumerable<string> GetEndpointWhitelist()
        {
            return _ipRateLimitOptions.EndpointWhitelist;
        }
    }

    public class RateLimitOptions
    {
        public bool EnableEndpointRateLimiting { get; set; }
        public bool StackBlockedRequests { get; set; }
        public string RealIpHeader { get; set; } = string.Empty;
        public string ClientIdHeader { get; set; } = string.Empty;
        public int HttpStatusCode { get; set; }
        public List<RateLimitRule> GeneralRules { get; set; } = new List<RateLimitRule>();
        public List<RateLimitRule> EndpointSpecificRules { get; set; } = new List<RateLimitRule>();
    }
} 