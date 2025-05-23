using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Base.Utilities
{
    public class BypassAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _defaultProvider;
        private readonly bool _bypassSecurity;

        public BypassAuthorizationPolicyProvider(
            IOptions<AuthorizationOptions> options, 
            IConfiguration configuration)
        {
            _defaultProvider = new DefaultAuthorizationPolicyProvider(options);
            _bypassSecurity = configuration.GetValue<bool>("Security:BypassSecurity");
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => 
            _bypassSecurity 
                ? Task.FromResult(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build())
                : _defaultProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => 
            _defaultProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (_bypassSecurity)
            {
                // Return a policy that allows all requests when security is bypassed
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true)
                    .Build();
                
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
            
            return _defaultProvider.GetPolicyAsync(policyName);
        }
    }
} 