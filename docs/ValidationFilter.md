# Model Validasyon Filtresi

`ValidationFilter` sınıfı, ASP.NET Core Web API projelerinde model validasyonunu otomatikleştiren ve standartlaştıran bir filter bileşenidir. Bu filter, HTTP isteklerindeki model validasyon hatalarını yakalar ve `ApiResponse` formatında tutarlı hata yanıtları döndürür.

## Genel Bakış

ValidationFilter, `Base.Utilities` namespace'i altında tanımlanmış olup aşağıdaki görevleri yerine getirir:

1. **Boş Request Kontrolü**: POST, PUT, PATCH isteklerinde boş body kontrolü yapar
2. **Model Validasyon Hataları**: ModelState üzerinden validasyon hatalarını tespit eder
3. **Zenginleştirilmiş Validasyon**: Reflection kullanarak validasyon attribute'larını kontrol eder
4. **Standart Hata Formatı**: Tüm validasyon hatalarını `ApiResponse` formatında döndürür
5. **Yetkilendirme Hataları**: 401 ve 403 hatalarını standart formata dönüştürür

## Kullanım

ValidationFilter, `Program.cs` içinde global olarak tüm controller'lara uygulanır:

```csharp
builder.Services.AddControllers(options =>
{
    // Validasyon filtresi ekle
    options.Filters.Add<ValidationFilter>();
    // ASP.NET Core'un varsayılan model doğrulama davranışını devre dışı bırak
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.ConfigureApiBehaviorOptions(options =>
{
    // Varsayılan validasyon davranışını devre dışı bırak
    options.SuppressModelStateInvalidFilter = true;
});
```

## Çalışma Mantığı

ValidationFilter, `IAsyncActionFilter` arayüzünü implemente eder ve her HTTP isteği işlenirken çalışır. Ana metodu `OnActionExecutionAsync` olup, validasyon süreci adım adım şu şekilde ilerler:

### 1. ADIM: Boş Request Kontrolü

POST, PUT ve PATCH isteklerinde request body'nin tamamen boş olup olmadığını kontrol eder:

```csharp
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
```

### 2. ADIM: Model Validasyon Hatalarını İşleme

ModelState geçersiz ise (validasyon hataları var ise), hataları toplar ve standart formatta döndürür:

```csharp
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
    // ...

    // 3. ADIM: Standart ApiResponse formatında hata yanıtı oluştur
    var response = ApiResponse<object>.Error(
        errors, 
        "Lütfen form alanlarını kontrol ediniz"
    );
    
    // 4. ADIM: 400 Bad Request olarak yanıt döndür
    context.Result = new BadRequestObjectResult(response);
    return;
}
```

### Zenginleştirilmiş Validasyon (2.2 ADIM)

Kodun en güçlü özelliklerinden biri, reflection kullanarak model üzerindeki tüm validasyon kurallarını kontrol etmesidir:

```csharp
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
        
        // ... veri tipine göre validasyon mantığı ...
    }
}
```

### 5. ADIM: Validasyonu Geçen İşlemleri Yönetme

Validasyon başarılı ise, işlem devam eder ve sonuç alınır. Ayrıca yetkilendirme hataları da standart formatta dönüştürülür:

```csharp
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
```

### Yardımcı Metodlar

Filter içerisinde, validasyon için kullanılan özel yardımcı metodlar da mevcuttur:

```csharp
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
```

## Hata Mesajları Formatı

ValidationFilter tarafından döndürülen hata yanıtları, tutarlı bir format izler:

```json
{
  "statusCode": 400,
  "isSuccess": false,
  "data": null,
  "errors": {
    "name": [
      "İsim alanı zorunludur.",
      "İsim en az 3 karakter olmalıdır."
    ],
    "email": [
      "Geçerli bir e-posta adresi giriniz."
    ]
  },
  "message": "Lütfen form alanlarını kontrol ediniz"
}
```

## Performans İyileştirmeleri

ValidationFilter, aşağıdaki optimizasyonlar ile performans açısından verimlidir:

1. **Erken Dönüş**: Validasyon hataları tespit edildiğinde, işlem hemen sonlandırılır
2. **Yardımcı Metodlar**: `IsEmptyBody` ve `IsPostOrPutOrPatch` gibi yardımcı metodlar kod tekrarını önler
3. **Aynı Hata Mesajlarının Temizlenmesi**: Aynı hata mesajları `Distinct()` ile temizlenir
4. **Property Adı Uyumluluğu**: PascalCase ve camelCase arasında esnek eşleştirme yapılır

## Sonuç

ValidationFilter, model validasyon işlemlerini merkezi bir noktada toplayarak:

1. Controller kodlarını sadeleştirir
2. Tutarlı hata yanıtları sağlar
3. İstemci uygulamalarının hata işleme mantığını basitleştirir
4. Validasyon attribute'larının gücünü en üst düzeyde kullanır

ValidationFilter, `Base.Utilities` namespace'i altında bulunur ve Program.cs içinde global olarak tüm API controller'larına uygulanır. 