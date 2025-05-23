using Base.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Base.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestSecurityLINQController : ControllerBase
    {
        [HttpGet("public")]
        public ActionResult<ApiResponse<object>> PublicEndpoint()
        {
            return ApiResponse<object>.Success(new { message = "Bu endpoint herkese açıktır." }, "Başarılı erişim sağlandı.");
        }

        [HttpGet("user")]
        [Authorize(Policy = "RequireUserRole")]
        public ActionResult<ApiResponse<object>> UserEndpoint()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            return ApiResponse<object>.Success(
                new { message = $"Merhaba {userName}, bu endpoint sadece User rolüne sahip kullanıcılar içindir." },
                "User rolü doğrulandı."
            );
        }

        [HttpGet("developer")]
        [Authorize(Policy = "RequireDeveloperRole")]
        public ActionResult<ApiResponse<object>> DeveloperEndpoint()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            return ApiResponse<object>.Success(
                new { message = $"Merhaba {userName}, bu endpoint sadece Developer rolüne sahip kullanıcılar içindir." },
                "Developer rolü doğrulandı."
            );
        }

        [HttpGet("admin")]
        [Authorize(Policy = "RequireAdminRole")]
        public ActionResult<ApiResponse<object>> AdminEndpoint()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return ApiResponse<object>.Success(
                new { message = $"Merhaba {userName} ({role}), bu endpoint sadece Admin ve SuperAdmin rollerine sahip kullanıcılar içindir." },
                "Admin/SuperAdmin rolü doğrulandı."
            );
        }

        [HttpGet("superadmin")]
        [Authorize(Policy = "RequireSuperAdminRole")]
        public ActionResult<ApiResponse<object>> SuperAdminEndpoint()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            return ApiResponse<object>.Success(
                new { message = $"Merhaba {userName}, bu endpoint sadece SuperAdmin rolüne sahip kullanıcılar içindir." },
                "SuperAdmin rolü doğrulandı."
            );
        }

        [HttpGet("user-info")]
        [Authorize]
        public ActionResult<ApiResponse<object>> GetUserInfo()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return this.UnauthorizedResponse<object>("Kimlik doğrulaması başarısız.");
            }

            var claims = identity.Claims;
            var userInfo = new
            {
                Username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                Role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value,
                UserId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                Claims = claims.Select(c => new { c.Type, c.Value })
            };

            return ApiResponse<object>.Success(userInfo, "Kullanıcı bilgileri başarıyla getirildi.");
        }
    }
} 