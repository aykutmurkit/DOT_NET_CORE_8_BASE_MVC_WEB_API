![ASP.NET Core Banner](assets/banner.png)

# ASP.NET Core BASE Web API Projesi Dokümantasyonu

## İçindekiler

1. [ApiResponse - Standart API Yanıt Formatı](docs/ApiResponse.md)
2. [Controller Extensions - Controller Genişletmeleri](docs/ControllerExtensions.md)
3. [ValidationFilter - Validasyon İşlemleri](docs/ValidationFilter.md)
4. [Proje Mimarisi](docs/Architecture.md)
5. [DTO Pattern - Veri Transfer Nesneleri](docs/DTOs.md)
6. [Veritabanı Seed İşlemi](docs/Seeding.md)

## Genel Bakış

Bu API projesi, modern yazılım geliştirme prensiplerini takip eden, temiz ve bakımı kolay bir mimariye sahiptir. Projede aşağıdaki teknolojiler ve yaklaşımlar kullanılmaktadır:

- **.NET 8**: Modern, hızlı ve çapraz platform destekli web framework
- **MVC Pattern**: Model-View-Controller mimari desenini kullanarak kodu organize eder
- **Entity Framework Core**: ORM (Object-Relational Mapping) aracı
- **SQL Server**: Veritabanı sistemi
- **JWT Authentication**: Kimlik doğrulama ve yetkilendirme
- **RESTful API**: HTTP protokolü üzerinden kaynak odaklı API tasarımı

## Teknoloji Tercihleri

Projede bilinçli olarak aşağıdaki teknolojiler kullanılmamıştır:

- **Fluent API**: Entity Framework Core ilişkileri ve yapılandırmaları için Data Annotations kullanılmıştır
- **Fluent Validation**: Model validasyonu için özel bir ValidationFilter ve Data Annotations kullanılmıştır
- **AutoMapper**: DTO dönüşümleri için manuel mapping kullanılmıştır

Bu tercihler, projenin daha az bağımlılığa sahip olması ve temel ASP.NET Core özelliklerinin daha iyi anlaşılması amacıyla yapılmıştır.

## Proje Yapısı

Proje, MVC (Model-View-Controller) desenine uygun olarak aşağıdaki ana dizinlerden oluşur:

- **Base/**: Ana proje dizini
  - **Controllers/**: API endpoint'lerini içeren controller'lar (Controller katmanı)
    - DeviceController.cs
    - DeviceLogController.cs
    - SecuredController.cs
    - TestValidationController.cs
  - **Models/**: Veritabanı entity'leri ve DTOs (Model katmanı)
    - Device.cs
    - DeviceLog.cs
    - JwtSettings.cs
    - UserRole.cs
    - DTOs/: Data Transfer Objects
  - **Views/**: MVC yapısında kullanılan view dosyaları (View katmanı - Web API'de minimal kullanım)
  - **Data/**: Veritabanı işlemleri ve context sınıfları
    - Context/: DbContext ve ilgili yapılandırmalar
    - Seeding/: Veritabanı seed işlemleri
  - **Utilities/**: Yardımcı sınıflar ve extension'lar
    - ApiResponse.cs
    - ValidationFilter.cs
    - ControllerExtensions.cs

## API Endpoint'leri

Projede aşağıdaki temel API endpoint'leri bulunmaktadır:

- **GET /api/device**: Tüm cihazları listeler
- **GET /api/device/{id}**: Belirli bir cihazın detaylarını getirir
- **POST /api/device**: Yeni bir cihaz oluşturur
- **PUT /api/device/{id}**: Cihaz bilgilerini günceller
- **DELETE /api/device/{id}**: Cihazı siler
- **GET /api/devicelog**: Tüm cihaz loglarını listeler
- **GET /api/devicelog/{id}**: Belirli bir cihaz logunu getirir
- **POST /api/devicelog**: Yeni bir cihaz logu oluşturur

## JWT Kimlik Doğrulama

Proje, JWT (JSON Web Token) tabanlı kimlik doğrulama kullanır. Kullanıcı rolleri aşağıdaki gibi tanımlanmıştır:

- **User**: Temel kullanıcı rolü
- **Developer**: Geliştirici rolü
- **Admin**: Yönetici rolü
- **SuperAdmin**: Süper yönetici rolü

Authorization politikaları şu şekilde tanımlanmıştır:
- "RequireUserRole": User rolü gerektirir
- "RequireDeveloperRole": Developer rolü gerektirir
- "RequireAdminRole": Admin veya SuperAdmin rolü gerektirir
- "RequireSuperAdminRole": SuperAdmin rolü gerektirir

## API Yanıt Formatı

Tüm API endpoint'leri, tutarlı bir yanıt formatı kullanır:

```json
{
  "statusCode": 200,
  "isSuccess": true,
  "data": { ... },
  "errors": null,
  "message": "İşlem başarıyla tamamlandı."
}
```

Bu format hakkında daha fazla bilgi için [ApiResponse](docs/ApiResponse.md) sayfasını inceleyebilirsiniz.

## Validasyon

Model validasyonu, `ValidationFilter` sınıfı kullanılarak merkezi olarak yapılır. Bu filter, tüm controller'lara global olarak uygulanır ve validasyon hatalarını standart API yanıt formatında döndürür. Detaylı bilgi için [ValidationFilter](docs/ValidationFilter.md) sayfasını inceleyebilirsiniz.

## Veritabanı Yapılandırması

Veritabanı yapılandırması `appsettings.json` dosyasında tanımlanmıştır:

```json
"Database": {
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Drop": {
    "Startup": false
  },
  "Seed": {
    "EnableSeeding": true
  }
}
```

- **ConnectionStrings:DefaultConnection**: Veritabanı bağlantı dizesi
- **Drop:Startup**: Uygulama başlatıldığında veritabanının silinip yeniden oluşturulmasını kontrol eder
- **Seed:EnableSeeding**: Seed işleminin etkinleştirilmesini kontrol eder

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır. 