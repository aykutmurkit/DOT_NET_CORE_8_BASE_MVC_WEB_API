# Rate Limiting Manual

## Overview

This custom rate limiting implementation allows for flexible request throttling based on IP address and endpoint paths. Rate limiting is essential for:
- Preventing abuse and DoS attacks
- Ensuring fair resource usage
- Protecting sensitive endpoints
- Maintaining application performance

## Configuration

Rate limiting settings are defined in the `appsettings.json` file under two main sections:

1. `RateLimitOptions`: Controls endpoint-specific rate limiting
2. `IpRateLimiting`: Controls IP-based rate limiting with additional options

### General Settings

```json
"RateLimitOptions": {
  "EnableEndpointRateLimiting": true,
  "StackBlockedRequests": false,
  "RealIpHeader": "X-Real-IP",
  "ClientIdHeader": "X-ClientId",
  "HttpStatusCode": 429,
  "GeneralRules": [...],
  "EndpointSpecificRules": [...]
}
```

- `EnableEndpointRateLimiting`: Activates the rate limiting system
- `StackBlockedRequests`: When true, subsequent requests will be queued if rate limit is exceeded
- `RealIpHeader`: Custom header for getting client IP behind proxies
- `ClientIdHeader`: Header for identifying clients by a custom ID
- `HttpStatusCode`: Response code when rate limit is exceeded (429 = Too Many Requests)

### Rate Limiting Rules

Rules define how many requests can be made to an endpoint in a specific time period:

```json
"GeneralRules": [
  {
    "Endpoint": "*",
    "Period": "1m",
    "Limit": 30
  }
]
```

- `Endpoint`: The path to limit (`*` = all endpoints)
- `Period`: Time window for counting requests (s = seconds, m = minutes, h = hours, d = days)
- `Limit`: Maximum allowed requests in the period

### Endpoint-Specific Rules

You can define stricter or more lenient limits for specific endpoints:

```json
"EndpointSpecificRules": [
  {
    "Endpoint": "GET:/api/RateLimitLINQ/test",
    "Period": "30s",
    "Limit": 5
  },
  {
    "Endpoint": "GET:/api/RateLimitLINQ/fast",
    "Period": "10s",
    "Limit": 2
  }
]
```

### IP Whitelisting

Configure IP addresses that should bypass rate limiting:

```json
"IpRateLimiting": {
  "IpWhitelist": [ "127.0.0.1", "::1/10", "192.168.0.0/24" ]
}
```

### Endpoint Whitelisting

Exclude specific endpoints from rate limiting:

```json
"EndpointWhitelist": [ "GET:/api/TestSecurityLINQ/public", "*:/api/status" ]
```

## Adding New Rate Limits

To add rate limits for a new endpoint:

1. Open `appsettings.json`
2. Find the `EndpointSpecificRules` section under `RateLimitOptions`
3. Add a new entry with this format:

```json
{
  "Endpoint": "METHOD:/path/to/endpoint",
  "Period": "timeUnit",
  "Limit": numberOfRequests
}
```

Example:
```json
{
  "Endpoint": "POST:/api/users",
  "Period": "1h",
  "Limit": 10
}
```

## Time Period Formats

- Seconds: `1s`, `5s`, `30s`, etc.
- Minutes: `1m`, `5m`, `15m`, etc.
- Hours: `1h`, `6h`, `12h`, etc.
- Days: `1d`, `7d`, `30d`, etc.

## Testing Rate Limiting

The application includes a dedicated controller for testing and monitoring rate limiting:

### Available Test Endpoints

- `/api/RateLimitLINQ/test`: General test endpoint (5 requests per 30 seconds)
- `/api/RateLimitLINQ/fast`: Strict rate limit test (2 requests per 10 seconds)
- `/api/RateLimitLINQ/config`: View current rate limit configuration
- `/api/RateLimitLINQ/info`: Get information about rate limiting usage

### How to Test

1. Make repeated requests to `/api/RateLimitLINQ/fast`
2. After 2 requests in 10 seconds, you should receive a 429 response
3. Wait for the cooldown period and try again

### Response Format

When rate limit is exceeded, the server returns:
- HTTP Status Code: 429 Too Many Requests
- Custom response headers:
  - `X-RateLimit-Limit`: The rate limit ceiling
  - `X-RateLimit-RetryAfter`: Seconds until the client can retry
- JSON response in ApiResponse format:

```json
{
  "success": false,
  "data": null,
  "errors": {
    "RateLimit": [
      "Rate limit of 2 requests per 10s exceeded."
    ]
  },
  "message": "Bu endpoint için istek limiti aşıldı. Lütfen 10s içinde en fazla 2 istek gönderin.",
  "statusCode": 429
}
```

This matches the format of other error responses (401, 403) in the application, providing consistency in error handling.

### Rate Limit Headers on Successful Responses

All successful responses include rate limit information in headers:
- `X-RateLimit-Limit`: Maximum requests allowed in the period
- `X-RateLimit-Remaining`: Remaining requests in the current period

## Implementation Details

The rate limiting system uses a custom middleware that:

1. Intercepts all incoming requests
2. Checks if the client IP is whitelisted
3. Determines if the requested endpoint has specific rate limit rules
4. Applies either specific rules or falls back to general rules
5. Tracks request counts in memory with proper time windows
6. Rejects requests that exceed defined limits

The middleware is registered in `Program.cs` and runs before authentication and authorization.

## Logging

Rate limiting events are logged to help monitor usage and potential abuse:
- Successful requests log the current count toward the limit
- Rejected requests log a warning with details about the exceeded limit

## Customization

For advanced customization, you can modify:
- `CustomRateLimitMiddleware.cs`: The core rate limiting implementation
- `RateLimitingService.cs`: Service for accessing rate limiting configuration 