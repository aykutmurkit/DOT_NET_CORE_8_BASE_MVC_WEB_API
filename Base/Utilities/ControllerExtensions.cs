using Microsoft.AspNetCore.Mvc;

namespace Base.Utilities
{
    /// <summary>
    /// Controller sınıfları için genişletme metodları
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// 201 Created durum kodu ile yanıt döner
        /// </summary>
        public static ActionResult<ApiResponse<T>> CreatedResponse<T>(this ControllerBase controller, T data, string message = "Kayıt başarıyla oluşturuldu")
        {
            var response = ApiResponse<T>.Created(data, message);
            return controller.StatusCode(201, response);
        }

        /// <summary>
        /// 204 No Content durum kodu ile yanıt döner
        /// </summary>
        public static ActionResult<ApiResponse<object>> NoContentResponse(this ControllerBase controller, string message = "Kayıt başarıyla silindi")
        {
            var response = ApiResponse<object>.NoContent(message);
            return controller.StatusCode(204, response);
        }

        /// <summary>
        /// 400 Bad Request durum kodu ile yanıt döner
        /// </summary>
        public static ActionResult<ApiResponse<T>> BadRequestResponse<T>(this ControllerBase controller, string message)
        {
            var response = ApiResponse<T>.Error(message, 400);
            return controller.BadRequest(response);
        }

        /// <summary>
        /// 401 Unauthorized durum kodu ile yanıt döner
        /// </summary>
        public static ActionResult<ApiResponse<T>> UnauthorizedResponse<T>(this ControllerBase controller, string message = "Bu işlem için giriş yapmanız gerekiyor")
        {
            var response = ApiResponse<T>.Unauthorized(message);
            return controller.StatusCode(401, response);
        }

        /// <summary>
        /// 403 Forbidden durum kodu ile yanıt döner
        /// </summary>
        public static ActionResult<ApiResponse<T>> ForbiddenResponse<T>(this ControllerBase controller, string message = "Bu işlem için yetkiniz bulunmuyor")
        {
            var response = ApiResponse<T>.Forbidden(message);
            return controller.StatusCode(403, response);
        }

        /// <summary>
        /// 404 Not Found durum kodu ile yanıt döner
        /// </summary>
        public static ActionResult<ApiResponse<T>> NotFoundResponse<T>(this ControllerBase controller, string message = "Aradığınız kayıt bulunamadı")
        {
            var response = ApiResponse<T>.NotFound(message);
            return controller.NotFound(response);
        }

        /// <summary>
        /// 409 Conflict durum kodu ile yanıt döner
        /// </summary>
        public static ActionResult<ApiResponse<T>> ConflictResponse<T>(this ControllerBase controller, string message = "Bu işlem mevcut bir kayıt ile çakışıyor")
        {
            var response = ApiResponse<T>.Conflict(message);
            return controller.StatusCode(409, response);
        }

        /// <summary>
        /// 500 Internal Server Error durum kodu ile yanıt döner
        /// </summary>
        public static ActionResult<ApiResponse<T>> ServerErrorResponse<T>(this ControllerBase controller, string message = "Sunucu hatası oluştu")
        {
            var response = ApiResponse<T>.ServerError(message);
            return controller.StatusCode(500, response);
        }
    }
} 