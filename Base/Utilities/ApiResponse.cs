namespace Base.Utilities
{
    /// <summary>
    /// Standart API yanıt sınıfı
    /// </summary>
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; }
        public string Message { get; set; }

        /// <summary>
        /// Başarılı yanıt oluşturur (200 OK)
        /// </summary>
        public static ApiResponse<T> Success(T data, string message = null, int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Data = data,
                StatusCode = statusCode,
                IsSuccess = true,
                Message = message
            };
        }

        /// <summary>
        /// Yeni kaynak oluşturulduğunda başarılı yanıt oluşturur (201 Created)
        /// </summary>
        public static ApiResponse<T> Created(T data, string message = "Kayıt başarıyla oluşturuldu")
        {
            return new ApiResponse<T>
            {
                Data = data,
                StatusCode = 201,
                IsSuccess = true,
                Message = message
            };
        }

        /// <summary>
        /// Kaynak başarıyla silindiğinde yanıt oluşturur (204 No Content)
        /// </summary>
        public static ApiResponse<T> NoContent(string message = "Kayıt başarıyla silindi")
        {
            return new ApiResponse<T>
            {
                Data = default,
                StatusCode = 204,
                IsSuccess = true,
                Message = message
            };
        }

        /// <summary>
        /// Hata yanıtı oluşturur (validation hataları için) (400 Bad Request)
        /// </summary>
        public static ApiResponse<T> Error(Dictionary<string, List<string>> errors, string message = "Lütfen form alanlarını kontrol ediniz", int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Errors = errors,
                StatusCode = statusCode,
                IsSuccess = false,
                Message = message
            };
        }

        /// <summary>
        /// Hata yanıtı oluşturur (tek hata mesajı için) (400 Bad Request)
        /// </summary>
        public static ApiResponse<T> Error(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                Message = message,
                StatusCode = statusCode,
                IsSuccess = false
            };
        }

        /// <summary>
        /// Kimlik doğrulama hatası yanıtı oluşturur (401 Unauthorized)
        /// </summary>
        public static ApiResponse<T> Unauthorized(string message = "Bu işlem için giriş yapmanız gerekiyor")
        {
            return Error(message, 401);
        }

        /// <summary>
        /// Yetki hatası yanıtı oluşturur (403 Forbidden)
        /// </summary>
        public static ApiResponse<T> Forbidden(string message = "Bu işlem için yetkiniz bulunmuyor")
        {
            return Error(message, 403);
        }

        /// <summary>
        /// Kaynak bulunamadı hatası yanıtı oluşturur (404 Not Found)
        /// </summary>
        public static ApiResponse<T> NotFound(string message = "Aradığınız kayıt bulunamadı")
        {
            return Error(message, 404);
        }

        /// <summary>
        /// Çakışma hatası yanıtı oluşturur (409 Conflict)
        /// </summary>
        public static ApiResponse<T> Conflict(string message = "Bu işlem mevcut bir kayıt ile çakışıyor")
        {
            return Error(message, 409);
        }

        /// <summary>
        /// Sunucu hatası yanıtı oluşturur (500 Internal Server Error)
        /// </summary>
        public static ApiResponse<T> ServerError(string message = "Sunucu hatası oluştu")
        {
            return Error(message, 500);
        }
    }
} 