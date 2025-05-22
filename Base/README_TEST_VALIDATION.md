# Test Validation Kullanımı

Bu belge, Test entity'si ve DTO'larındaki validasyonların nasıl test edileceğini açıklar.

## Test DTO'ları

### 1. TestCreateDto
Bu DTO, yeni bir test kaydı oluşturmak için kullanılır ve şu validasyonları içerir:

1. **Name:**
   - Zorunlu alan
   - 3-100 karakter arasında olmalı
   - Yalnızca harf, rakam, boşluk ve tire içerebilir

2. **Description:**
   - En fazla 500 karakter olabilir
   - En az 10 karakter olmalı
   - Metin formatında olmalı

3. **Value:**
   - Zorunlu alan
   - 0.01 ile 999999.99 arasında olmalı
   - Sayısal formatta olmalı

### 2. TestUpdateDto
Bu DTO, mevcut bir test kaydını güncellemek için kullanılır ve şu validasyonları içerir:

1. **Id:**
   - Zorunlu alan
   - Pozitif bir sayı olmalı
   - Var olan bir kayda referans vermeli (1-5 arası ID'ler kabul edilir)

2. **Name:**
   - Zorunlu alan
   - 3-100 karakter arasında olmalı
   - Yalnızca harf, rakam, boşluk ve tire içerebilir

3. **Description:**
   - En fazla 500 karakter olabilir
   - En az 10 karakter olmalı
   - Metin formatında olmalı

4. **Value:**
   - Zorunlu alan
   - 0.01 ile 999999.99 arasında olmalı
   - Sayısal formatta olmalı

## Test Endpoint'leri

### 1. Örnek Verileri Görüntüleme
```
GET /api/TestValidation/samples
```

Bu endpoint, test için kullanabileceğiniz geçerli ve geçersiz JSON örneklerini döndürür.

### 2. Create DTO Validasyonu
```
POST /api/TestValidation/create/test
```

Bu endpoint'e bir TestCreateDto nesnesi gönderdiğinizde validasyon sonuçlarını görebilirsiniz.

### 3. Update DTO Validasyonu
```
POST /api/TestValidation/update/test
```

Bu endpoint'e bir TestUpdateDto nesnesi gönderdiğinizde validasyon sonuçlarını görebilirsiniz.

## Test Örnekleri

### Geçerli Create İsteği
```json
{
  "name": "Test Örneği",
  "description": "Bu bir test örneğidir. En az 10 karakter.",
  "value": 99.99
}
```

### Geçerli Update İsteği
```json
{
  "id": 1,
  "name": "Test Örneği Güncel",
  "description": "Bu güncellenmiş bir test örneğidir. En az 10 karakter.",
  "value": 149.99
}
```

### Geçersiz Create İsteği (Tüm validasyonlar başarısız)
```json
{
  "name": "A",
  "description": "Kısa",
  "value": 0
}
```

### Geçersiz Update İsteği (Tüm validasyonlar başarısız)
```json
{
  "id": 99,
  "name": "A@#$%",
  "description": "AAAAA... (501 karakter)",
  "value": 1000000
}
```

## Yanıt Örnekleri

### Başarılı Validasyon Yanıtı
```json
{
  "statusCode": 200,
  "isSuccess": true,
  "data": {
    "isValidationPassed": true,
    "sentData": {
      "name": "Test Örneği",
      "description": "Bu bir test örneğidir. En az 10 karakter.",
      "value": 99.99
    }
  },
  "errors": null,
  "message": "Test create validasyonu başarılı."
}
```

### Başarısız Validasyon Yanıtı (Örnek)
```json
{
  "statusCode": 400,
  "isSuccess": false,
  "data": null,
  "errors": {
    "Name": ["Test adı 3-100 karakter arasında olmalıdır."],
    "Description": ["Açıklama en az 10 karakter olmalıdır."],
    "Value": ["Değer 0.01 ile 999999.99 arasında olmalıdır."]
  },
  "message": "Validasyon hataları oluştu."
}
```

## Notlar

- Bu controller sadece test amaçlıdır ve gerçek bir veritabanı işlemi gerçekleştirmez.
- Update işleminde ID kontrolü, gerçek veritabanı yerine basit bir koşulla simüle edilmektedir (1-5 arası ID'ler kabul edilir).
- Tüm endpoint'ler ApiResponse formatında standart bir yanıt döndürür.
- Bu testleri Postman veya Swagger UI üzerinden kolayca gerçekleştirebilirsiniz. 