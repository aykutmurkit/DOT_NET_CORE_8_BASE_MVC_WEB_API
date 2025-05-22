# Controller Extension Metodları

Controller extension'lar, ASP.NET Core'daki controller sınıflarına ek işlevsellik kazandıran extension metodlarıdır. Bu metodlar, kod tekrarını azaltır ve daha temiz bir kodlama stili sağlar.

## Avantajları

- **Kod Tekrarını Azaltır**: Yaygın kullanılan yanıt kodlarını tek bir yerde tanımlarsınız
- **Tutarlılık**: Tüm controller'larda aynı yanıt formatını kullanmayı kolaylaştırır
- **Okunabilirlik**: Controller metodlarını daha kısa ve anlaşılır hale getirir
- **Bakım Kolaylığı**: Değişiklikler tek bir yerde yapılır ve tüm projeye uygulanır

## Örnek Controller Extension Sınıfı

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Base.Utilities
{
    public static class ControllerExtensions
    {
        // 400 Bad Request yanıtı oluşturur
        public static ActionResult<ApiResponse<T>> BadRequestResponse<T>(this ControllerBase controller, string message)
        {
            var response = ApiResponse<T>.Error(message, 400);
            return controller.BadRequest(response);
        }

        // 404 Not Found yanıtı oluşturur
        public static ActionResult<ApiResponse<T>> NotFoundResponse<T>(this ControllerBase controller, string message)
        {
            var response = ApiResponse<T>.NotFound(message);
            return controller.NotFound(response);
        }

        // 201 Created yanıtı oluşturur
        public static ActionResult<ApiResponse<T>> CreatedResponse<T>(this ControllerBase controller, T data, string message)
        {
            var response = ApiResponse<T>.Created(data, message);
            return controller.StatusCode(201, response);
        }

        // 204 No Content yanıtı oluşturur
        public static ActionResult<ApiResponse<object>> NoContentResponse(this ControllerBase controller, string message)
        {
            var response = ApiResponse<object>.NoContent(message);
            return controller.StatusCode(204, response);
        }

        // 500 Server Error yanıtı oluşturur
        public static ActionResult<ApiResponse<T>> ServerErrorResponse<T>(this ControllerBase controller, string message)
        {
            var response = ApiResponse<T>.ServerError(message);
            return controller.StatusCode(500, response);
        }
    }
}
```

## Kullanım Örnekleri

### 1. 404 Not Found Yanıtı

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<Device>>> GetById(int id)
{
    var device = await _context.Devices.FindAsync(id);
    
    if (device == null)
    {
        // Extension metod kullanımı
        return this.NotFoundResponse<Device>("Cihaz bulunamadı.");
    }
    
    return ApiResponse<Device>.Success(device, "Cihaz başarıyla getirildi.");
}
```

### 2. 201 Created Yanıtı

```csharp
[HttpPost]
public async Task<ActionResult<ApiResponse<Device>>> Create(Device device)
{
    device.CreatedDate = DateTime.Now;
    _context.Devices.Add(device);
    await _context.SaveChangesAsync();
    
    // Extension metod kullanımı
    return this.CreatedResponse(device, "Cihaz başarıyla oluşturuldu.");
}
```

### 3. 400 Bad Request Yanıtı

```csharp
[HttpPut("{id}")]
public async Task<ActionResult<ApiResponse<Device>>> Update(int id, Device device)
{
    if (id != device.Id)
    {
        // Extension metod kullanımı
        return this.BadRequestResponse<Device>("ID eşleşmiyor.");
    }
    
    // Diğer işlemler...
}
```

## Uygulama Yöntemleri

1. **Projeye Uygun Metodlar Ekleyin**: Projenizin ihtiyaçlarına göre yeni extension metodlar ekleyebilirsiniz
2. **Tutarlı Kullanım**: Tüm controller'larda bu extension metodları kullanmaya özen gösterin
3. **ApiResponse ile Entegrasyon**: Extension metodlarınızı ApiResponse sınıfınızla uyumlu olacak şekilde tasarlayın
4. **Dökümantasyon**: Extension metodlarınızı XML dökümantasyonu ile açıklayın

## İpuçları

- Extension metodlarınızı Utilities veya Extensions adlı bir klasörde toplayın
- API'nizin kullandığı tüm HTTP durum kodları için extension metodlar oluşturun
- Metodlarınızı generic olarak tasarlayarak farklı veri tipleriyle çalışabilir hale getirin 