using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Threading;

namespace Base.Utilities
{
    public class CustomRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomRateLimitMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, RateLimitCounter> _counters;

        // Dictionary for tracking requests by endpoint and IP
        private class RateLimitCounter
        {
            public DateTime LastReset { get; set; }
            public int Count { get; set; }
        }

        public CustomRateLimitMiddleware(
            RequestDelegate next,
            ILogger<CustomRateLimitMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _counters = new ConcurrentDictionary<string, RateLimitCounter>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.Request.Path.ToString();
            var method = context.Request.Method;
            var ip = GetClientIpAddress(context);
            var rateLimitOptions = _configuration.GetSection("RateLimitOptions").Get<RateLimitOptions>();

            if (rateLimitOptions?.EnableEndpointRateLimiting == true)
            {
                // Check for whitelist IPs
                if (IsIpWhitelisted(ip))
                {
                    await _next(context);
                    return;
                }

                // Find a matching rule
                RateLimitRule? matchingRule = null;

                // Check endpoint-specific rules first
                if (rateLimitOptions.EndpointSpecificRules != null)
                {
                    matchingRule = rateLimitOptions.EndpointSpecificRules.FirstOrDefault(r =>
                    {
                        var parts = r.Endpoint.Split(':');
                        if (parts.Length == 2)
                        {
                            var ruleMethod = parts[0];
                            var rulePath = parts[1];

                            return (ruleMethod == "*" || ruleMethod.Equals(method, StringComparison.OrdinalIgnoreCase)) &&
                                   (rulePath == "*" || endpoint.StartsWith(rulePath, StringComparison.OrdinalIgnoreCase));
                        }
                        return false;
                    });
                }

                // If no specific rule matches, use general rules
                if (matchingRule == null && rateLimitOptions.GeneralRules != null)
                {
                    matchingRule = rateLimitOptions.GeneralRules.FirstOrDefault();
                }

                if (matchingRule != null)
                {
                    var key = $"{ip}_{method}_{endpoint}_{matchingRule.Period}";
                    if (!_counters.TryGetValue(key, out var counter))
                    {
                        counter = new RateLimitCounter
                        {
                            LastReset = DateTime.UtcNow,
                            Count = 0
                        };
                    }

                    // Check if we need to reset counter based on period
                    var period = ParsePeriod(matchingRule.Period);
                    if (DateTime.UtcNow - counter.LastReset > period)
                    {
                        counter.Count = 0;
                        counter.LastReset = DateTime.UtcNow;
                    }

                    // Check if limit has been exceeded
                    if (counter.Count >= matchingRule.Limit)
                    {
                        _logger.LogWarning($"Rate limit exceeded for {ip} on {method}:{endpoint}. Limit: {matchingRule.Limit} per {matchingRule.Period}");
                        
                        // Return custom 429 Too Many Requests response in ApiResponse format
                        context.Response.StatusCode = rateLimitOptions.HttpStatusCode > 0 ? rateLimitOptions.HttpStatusCode : 429;
                        context.Response.ContentType = "application/json";
                        
                        var response = ApiResponse<object>.Error(
                            new Dictionary<string, List<string>> {
                                { "RateLimit", new List<string> { $"Rate limit of {matchingRule.Limit} requests per {matchingRule.Period} exceeded." } }
                            },
                            $"Bu endpoint için istek limiti aşıldı. Lütfen {matchingRule.Period} içinde en fazla {matchingRule.Limit} istek gönderin.",
                            429 // Explicitly passing the 429 status code
                        );
                        
                        // Set custom response headers
                        context.Response.Headers["X-RateLimit-Limit"] = matchingRule.Limit.ToString();
                        context.Response.Headers["X-RateLimit-RetryAfter"] = period.TotalSeconds.ToString();
                        
                        await context.Response.WriteAsJsonAsync(response);
                        return;
                    }

                    // Increment counter and update dictionary
                    counter.Count++;
                    _counters[key] = counter;

                    // Add rate limit headers to successful responses
                    context.Response.OnStarting(() => {
                        context.Response.Headers["X-RateLimit-Limit"] = matchingRule.Limit.ToString();
                        context.Response.Headers["X-RateLimit-Remaining"] = (matchingRule.Limit - counter.Count).ToString();
                        return Task.CompletedTask;
                    });

                    _logger.LogInformation($"Request from {ip} to {method}:{endpoint} - {counter.Count}/{matchingRule.Limit} per {matchingRule.Period}");
                }
            }

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var ipOptions = _configuration.GetSection("IpRateLimiting").Get<IpRateLimitOptions>();
            var realIpHeader = ipOptions?.RealIpHeader ?? "X-Real-IP";

            string? ip = null;
            if (!string.IsNullOrEmpty(realIpHeader))
            {
                ip = context.Request.Headers[realIpHeader].FirstOrDefault();
            }

            return ip ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsIpWhitelisted(string ip)
        {
            var ipOptions = _configuration.GetSection("IpRateLimiting").Get<IpRateLimitOptions>();
            if (ipOptions?.IpWhitelist == null || !ipOptions.IpWhitelist.Any())
            {
                return false;
            }

            return ipOptions.IpWhitelist.Contains(ip) || 
                  ipOptions.IpWhitelist.Any(range => ip.StartsWith(range.TrimEnd('*')));
        }

        private TimeSpan ParsePeriod(string period)
        {
            if (string.IsNullOrEmpty(period))
            {
                return TimeSpan.FromMinutes(1); // Default 1 minute
            }

            var timeValue = int.Parse(period.Substring(0, period.Length - 1));
            var timeUnit = period.Substring(period.Length - 1).ToLower();

            return timeUnit switch
            {
                "s" => TimeSpan.FromSeconds(timeValue),
                "m" => TimeSpan.FromMinutes(timeValue),
                "h" => TimeSpan.FromHours(timeValue),
                "d" => TimeSpan.FromDays(timeValue),
                _ => TimeSpan.FromMinutes(timeValue)
            };
        }
    }

    // Extension method for registering our custom middleware
    public static class CustomRateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomRateLimitMiddleware>();
        }
    }
} 