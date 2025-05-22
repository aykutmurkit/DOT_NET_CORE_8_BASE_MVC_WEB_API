# Validasyon Test Controller Kullanımı

Bu controller, DTO sınıflarındaki validasyonların doğru çalışıp çalışmadığını test etmek için oluşturulmuştur.

## Endpoint'ler

### 1. Geçersiz Örnek JSON Oluşturma
```
GET /api/ValidationTest/generate-invalid-samples
```

Bu endpoint, validasyon hatası oluşturacak örnek JSON nesneleri döner. Bu örnekleri diğer test endpoint'lerinde kullanabilirsiniz.

### 2. Device Create Validasyonu
```
POST /api/ValidationTest/device/create
```

Bu endpoint'e geçersiz bir DeviceCreateDto nesnesi gönderdiğinizde, validasyon hatalarını görebilirsiniz. Örnek istek:

```json
{
  "Description": "Test açıklaması",
  "IsActive": true
}
```

Dönen yanıt (validasyon hataları):
```json
{
  "statusCode": 400,
  "isSuccess": false,
  "data": null,
  "errors": {
    "Name": ["Cihaz adı zorunludur."]
  },
  "message": "Validasyon hataları oluştu."
}
```

### 3. Device Update Validasyonu
```
POST /api/ValidationTest/device/update
```

Bu endpoint'e geçersiz bir DeviceUpdateDto nesnesi gönderdiğinizde, validasyon hatalarını görebilirsiniz. Örnek istek:

```json
{
  "Id": 99999,
  "Name": "A",
  "Description": "AAAAA... (501 karakter)",
  "IsActive": true
}
```

### 4. DeviceLog Create Validasyonu
```
POST /api/ValidationTest/devicelog/create
```

Bu endpoint'e geçersiz bir DeviceLogCreateDto nesnesi gönderdiğinizde, validasyon hatalarını görebilirsiniz. Örnek istek:

```json
{
  "DeviceId": 99999,
  "Severity": 10,
  "IsResolved": true,
  "ResolutionNotes": "AAAAAA... (501 karakter)"
}
```

### 5. DeviceLog Update Validasyonu
```
POST /api/ValidationTest/devicelog/update
```

Bu endpoint'e geçersiz bir DeviceLogUpdateDto nesnesi gönderdiğinizde, validasyon hatalarını görebilirsiniz. Örnek istek:

```json
{
  "Id": 99999,
  "DeviceId": 99999,
  "Message": "A",
  "LogType": "AAAAA... (51 karakter)",
  "Severity": 10,
  "IsResolved": true,
  "ResolutionNotes": "AAAAAA... (501 karakter)"
}
```

## Test Senaryoları

1. **DeviceCreateDto İçin:**
   - Name alanını boş bırakma
   - Name alanını 3 karakterden az verme
   - Name alanını 100 karakterden fazla verme

2. **DeviceUpdateDto İçin:**
   - Id alanının olmayan bir kayda referans vermesi
   - Name validasyonları (CreateDto ile aynı)
   - Description alanının 500 karakterden fazla olması

3. **DeviceLogCreateDto İçin:**
   - DeviceId alanının olmayan bir cihaza referans vermesi
   - Message ve LogType alanlarını boş bırakma
   - Message alanını 2 karakterden az verme
   - LogType alanını 50 karakterden fazla verme
   - Severity alanını 1-5 aralığı dışında verme
   - ResolutionNotes alanının 500 karakterden fazla olması

4. **DeviceLogUpdateDto İçin:**
   - Id alanının olmayan bir kayda referans vermesi
   - DeviceId alanının olmayan bir cihaza referans vermesi
   - Diğer validasyonlar (CreateDto ile aynı)

## Notlar

- Bu controller sadece test amaçlıdır, production ortamında kullanılmamalıdır.
- ValidationTestController, DTO sınıflarındaki data annotation'ların doğru çalıştığını doğrulamak için kullanılabilir.
- ApiResponse<T> sınıfı ile HTTP 400 Bad Request yanıtları otomatik olarak strukturüzlenmiş ve detaylı hata mesajları içerecek şekilde formatlanmıştır.
- Bu testleri Postman veya Swagger UI üzerinden kolayca gerçekleştirebilirsiniz. 