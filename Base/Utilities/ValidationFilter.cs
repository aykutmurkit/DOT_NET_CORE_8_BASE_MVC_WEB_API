using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text;

namespace Base.Utilities
{
    /// <summary>
    /// Model validasyon hatalarını yakalayıp standart formatta dönüş sağlayan filtre.
    /// Bu filtre iki temel işlev sağlar:
    /// 1. Boş request kontrolü
    /// 2. Model validasyon hatalarını ApiResponse formatında döndürme
    /// </summary>
    public class ValidationFilter : IAsyncActionFilter
    {
        /// <summary>
        /// HTTP request işlenirken çalışan ana metot.
        /// İstek işlenirken validasyon hatalarını kontrol eder ve uygun yanıtı oluşturur.
        /// </summary>
        /// <param name="context">Action çalıştırma bağlamı</param>
        /// <param name="next">Bir sonraki filter'a geçiş için delegate</param>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. ADIM: Tamamen boş request kontrolü (body yok)
            // POST, PUT, PATCH isteklerinde body boş ise hata döndür
            if (IsEmptyBody(context))
            {
                var response = ApiResponse<object>.Error(
                    message: "Lütfen request body giriniz", 
                    statusCode: 400
                );
                
                context.Result = new BadRequestObjectResult(response);
                return;
            }
            
            // 2. ADIM: Model validasyon hatalarını kontrol et
            // ModelState.IsValid false ise validasyon hataları var demektir
            if (!context.ModelState.IsValid)
            {
                // Hata sözlüğü oluştur (anahtar: property adı, değer: hata mesajları listesi)
                var errors = new Dictionary<string, List<string>>();
                
                // 2.1 ADIM: ASP.NET Core tarafından zaten tespit edilmiş hataları topla
                foreach (var key in context.ModelState.Keys)
                {
                    if (context.ModelState[key].Errors.Count > 0)
                    {
                        errors[key] = context.ModelState[key].Errors
                            .Select(e => string.IsNullOrEmpty(e.ErrorMessage) 
                                ? "Geçersiz değer" 
                                : e.ErrorMessage)
                            .ToList();
                    }
                }

                // 2.2 ADIM: Model üzerinde reflection yaparak eksik/hatalı alanlar için tüm validasyon kurallarını kontrol et
                if (context.ActionArguments.Count > 0)
                {
                    foreach (var arg in context.ActionArguments.Values)
                    {
                        if (arg != null)
                        {
                            var modelType = arg.GetType();
                            var properties = modelType.GetProperties();

                            // Her bir property için validasyon hataları kontrol edilir
                            foreach (var prop in properties)
                            {
                                // 2.2.1 ADIM: Property adı için anahtar belirle (C# property adları Pascal case'dir)
                                // errors sözlüğünde property adı PascalCase veya camelCase olarak bulunabilir
                                var propKey = prop.Name;
                                var firstChar = propKey[0].ToString().ToLower();
                                var lowerFirstPropKey = firstChar + propKey.Substring(1);
                                
                                var key = errors.ContainsKey(propKey) ? propKey : 
                                         errors.ContainsKey(lowerFirstPropKey) ? lowerFirstPropKey : null;
                                
                                // 2.2.2 ADIM: Property değerini kontrol et (null mu veya default değerinde mi)
                                var propValue = prop.GetValue(arg);
                                bool isPropertyNull = propValue == null;
                                bool isValueTypeDefault = false;
                                
                                // Value type (int, decimal, bool, vb.) için default değer kontrolü
                                if (prop.PropertyType.IsValueType && propValue != null)
                                {
                                    isValueTypeDefault = propValue.Equals(Activator.CreateInstance(prop.PropertyType));
                                }
                                
                                // Bu property için tüm hata mesajlarını topla
                                var allErrors = new List<string>();
                                
                                // 2.2.3 ADIM: Mevcut hataları ekle (ModelState'de varsa)
                                if (key != null && errors.ContainsKey(key))
                                {
                                    allErrors.AddRange(errors[key]);
                                }
                                
                                // 2.2.4 ADIM: Eksik veya geçersiz alanlar için tüm validasyon kurallarını kontrol et
                                if (isPropertyNull || isValueTypeDefault || (key != null && errors.ContainsKey(key)))
                                {
                                    // Required attribute özel olarak işlenir
                                    var requiredAttr = prop.GetCustomAttribute<RequiredAttribute>();
                                    if (requiredAttr != null && (isPropertyNull || isValueTypeDefault))
                                    {
                                        allErrors.Add(requiredAttr.ErrorMessage ?? $"{prop.Name} alanı zorunludur.");
                                    }
                                    
                                    // Diğer tüm validasyon attribute'ları için
                                    var validationAttributes = prop.GetCustomAttributes<ValidationAttribute>()
                                        .Where(attr => !(attr is RequiredAttribute)); // Required attribute'ı zaten işledik
                                    
                                    foreach (var attr in validationAttributes)
                                    {
                                        // Validasyon mantığını property tipine göre belirle
                                        bool shouldAddError = false;
                                        
                                        // String property'leri için (null ise hepsini kontrol et)
                                        if (prop.PropertyType == typeof(string))
                                        {
                                            // String null ise tüm string validasyonlarını kontrol et
                                            // (StringLength, MinLength, MaxLength, RegularExpression, vb.)
                                            if (isPropertyNull)
                                            {
                                                shouldAddError = true;
                                            }
                                        }
                                        // Value type property'leri için (int, decimal, DateTime, vb.)
                                        else if (prop.PropertyType.IsValueType)
                                        {
                                            // Default değer ise tüm value type validasyonlarını kontrol et
                                            // (Range, vb.)
                                            if (isValueTypeDefault)
                                            {
                                                shouldAddError = true;
                                            }
                                        }
                                        // Diğer referans tipleri için (complex objects, collections, vb.)
                                        else
                                        {
                                            // Referans null ise kontrol et
                                            if (isPropertyNull)
                                            {
                                                shouldAddError = true;
                                            }
                                        }
                                        
                                        // ErrorMessage varsa ve validasyon kuralı uygulanabilirse ekle
                                        if (shouldAddError && !string.IsNullOrEmpty(attr.ErrorMessage))
                                        {
                                            allErrors.Add(attr.ErrorMessage);
                                        }
                                    }
                                }
                                
                                // 2.2.5 ADIM: Aynı hata mesajlarını temizle ve errors sözlüğüne ekle
                                if (allErrors.Count > 0)
                                {
                                    var propKeyToUse = key ?? propKey;
                                    errors[propKeyToUse] = allErrors.Distinct().ToList();
                                }
                            }
                        }
                    }
                }

                // 3. ADIM: Standart ApiResponse formatında hata yanıtı oluştur
                var response = ApiResponse<object>.Error(
                    errors, 
                    "Lütfen form alanlarını kontrol ediniz"
                );
                
                // 4. ADIM: 400 Bad Request olarak yanıt döndür
                context.Result = new BadRequestObjectResult(response);
                return;
            }

            // 5. ADIM: Validasyon başarılıysa, işleme devam et ve sonucu al
            var resultContext = await next();
            
            // 6. ADIM: Eğer result bir IStatusCodeActionResult ise, status code'u kontrol et
            if (resultContext.Result is IStatusCodeActionResult statusCodeResult)
            {
                var statusCode = statusCodeResult.StatusCode;
                
                // 401 Unauthorized (Yetkisiz erişim) hatası
                if (statusCode == (int)HttpStatusCode.Unauthorized)
                {
                    // Özel format: errors null, sadece mesaj içeren ApiResponse
                    var unauthorizedResponse = ApiResponse<object>.Unauthorized(
                        "Bu işlemi gerçekleştirmek için giriş yapmanız gerekmektedir"
                    );
                    
                    resultContext.Result = new ObjectResult(unauthorizedResponse)
                    {
                        StatusCode = (int)HttpStatusCode.Unauthorized
                    };
                }
                // 403 Forbidden (Yasaklı erişim) hatası
                else if (statusCode == (int)HttpStatusCode.Forbidden)
                {
                    // Özel format: errors null, sadece mesaj içeren ApiResponse
                    var forbiddenResponse = ApiResponse<object>.Forbidden(
                        "Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır"
                    );
                    
                    resultContext.Result = new ObjectResult(forbiddenResponse)
                    {
                        StatusCode = (int)HttpStatusCode.Forbidden
                    };
                }
            }
        }
        
        /// <summary>
        /// HTTP isteğinin tamamen boş olup olmadığını kontrol eder.
        /// POST, PUT, PATCH isteklerinde body olmaması durumunda true döner.
        /// </summary>
        private bool IsEmptyBody(ActionExecutingContext context)
        {
            if (IsPostOrPutOrPatch(context))
            {
                // Request ContentLength 0 ise tamamen boş demektir
                return context.HttpContext.Request.ContentLength == 0;
            }
            return false;
        }
        
        /// <summary>
        /// HTTP isteğinin POST, PUT veya PATCH olup olmadığını kontrol eder.
        /// Bu HTTP metodları body içermelidir, bu nedenle validasyon için önemlidir.
        /// </summary>
        private bool IsPostOrPutOrPatch(ActionExecutingContext context)
        {
            return context.HttpContext.Request.Method == "POST" || 
                   context.HttpContext.Request.Method == "PUT" ||
                   context.HttpContext.Request.Method == "PATCH";
        }
    }
} 