# ApiResponse Sınıfı

`ApiResponse<T>` sınıfı, API'ınızda tutarlı yanıt formatı sağlamak için kullanılan bir yardımcı sınıftır. Bu sınıf, tüm API yanıtlarınızı standart bir format içinde döndürmenizi sağlar.

## Avantajları

- **Tutarlılık**: Tüm endpoint'ler aynı yanıt formatını kullanır
- **Tip Güvenliği**: Generic yapısı ile herhangi bir veri tipi için kullanılabilir
- **Kolay Kullanım**: Başarı ve hata durumları için hazır metodlar içerir
- **İstemci Dostu**: Frontend uygulamaları için tutarlı bir sözleşme sağlar

## Yapısı

```csharp
public class ApiResponse<T>
{
    public int StatusCode { get; set; }        // HTTP Durum Kodu
    public bool IsSuccess { get; set; }        // İşlem başarılı mı?
    public T Data { get; set; }                // Döndürülen veri
    public Dictionary<string, List<string>> Errors { get; set; }  // Hata mesajları
    public string Message { get; set; }        // Bilgi mesajı
}
```

## Kullanım Örnekleri

### 1. Başarılı Yanıt Döndürme

#### Liste Döndürme

```csharp
[HttpGet]
public async Task<ActionResult<ApiResponse<List<Device>>>> GetAll()
{
    var devices = await _context.Devices.ToListAsync();
    return ApiResponse<List<Device>>.Success(devices, "Cihazlar başarıyla listelendi.");
}
```

#### Tek Obje Döndürme

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<Device>>> GetById(int id)
{
    var device = await _context.Devices.FindAsync(id);
    
    if (device == null)
    {
        return ApiResponse<Device>.NotFound("Cihaz bulunamadı.");
    }
    
    return ApiResponse<Device>.Success(device, "Cihaz başarıyla getirildi.");
}
```

### 2. Hata Yanıtı Döndürme

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponse<Device>>> GetById(int id)
{
    var device = await _context.Devices.FindAsync(id);
    
    if (device == null)
    {
        return ApiResponse<Device>.Error("Cihaz bulunamadı.", 404);
    }
    
    return ApiResponse<Device>.Success(device, "Cihaz başarıyla getirildi.");
}
```

### 3. Validasyon Hatalarını Döndürme

```csharp
if (!ModelState.IsValid)
{
    var errors = new Dictionary<string, List<string>>();
    
    foreach (var key in ModelState.Keys)
    {
        var errorMessages = ModelState[key].Errors
            .Select(e => e.ErrorMessage)
            .ToList();
            
        if (errorMessages.Any())
        {
            errors.Add(key, errorMessages);
        }
    }
    
    return BadRequest(ApiResponse<object>.Error(errors, "Validasyon hataları oluştu."));
}
```

### 4. Kimlik Doğrulama/Yetkilendirme Hataları

```csharp
// 401 Unauthorized
var unauthorizedResponse = ApiResponse<object>.Unauthorized(
    "Bu işlemi gerçekleştirmek için giriş yapmanız gerekmektedir");

// 403 Forbidden
var forbiddenResponse = ApiResponse<object>.Forbidden(
    "Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır");
```

## Diğer Özel Yanıt Metotları

```csharp
// 201 Created
ApiResponse<T>.Created(data, message);

// 204 No Content
ApiResponse<T>.NoContent(message);

// 404 Not Found
ApiResponse<T>.NotFound(message);

// 409 Conflict
ApiResponse<T>.Conflict(message);

// 500 Server Error