# Proje Mimarisi

Bu ASP.NET Core Web API projesi, temiz bir mimari yapısı kullanmaktadır. Proje, bakımı kolay, test edilebilir ve ölçeklenebilir bir yapıya sahiptir.

## Katmanlı Mimari

Proje, aşağıdaki katmanlardan oluşmaktadır:

### 1. Models (Veri Modelleri)

Bu katman, uygulamanın temel veri yapılarını içerir. Veritabanı tablolarını temsil eden entity sınıfları bu katmanda yer alır.

```
/Models
  ├── Device.cs          # Cihaz entity
  ├── DeviceLog.cs       # Cihaz log entity
  └── ...
```

### 2. Data (Veri Erişim Katmanı)

Bu katman, veritabanı işlemlerini gerçekleştiren sınıfları içerir.

```
/Data
  ├── /Context
  │     └── AppDbContext.cs    # Entity Framework DB Context
  └── /Seeding
        ├── ISeeder.cs          # Seeder arayüzü
        ├── DatabaseSeeder.cs   # Ana seeder yöneticisi
        ├── DeviceSeeder.cs     # Cihaz verilerini oluşturma
        └── DeviceLogSeeder.cs  # Cihaz log verilerini oluşturma
```

### 3. Controllers (API Endpoint'leri)

Bu katman, API endpoint'lerini içerir. Her controller, belirli bir entity veya iş mantığı grubu için CRUD işlemlerini ve diğer operasyonları sağlar.

```
/Controllers
  ├── DeviceController.cs     # Cihaz API
  ├── UserController.cs       # Kullanıcı API
  └── AuthController.cs       # Kimlik doğrulama API
```

### 4. Utilities (Yardımcı Sınıflar ve Filtreler)

Bu katman, tüm uygulama genelinde kullanılan yardımcı sınıfları ve filtreleri içerir.

```
/Utilities
  ├── ApiResponse.cs           # Standart API yanıt formatı
  ├── ControllerExtensions.cs  # Controller extension metodları
  └── ValidationFilter.cs      # Model validasyon filtresi
```

### 5. DTOs (Data Transfer Objects)

Bu katman, veri transfer nesnelerini içerir. Controller'larda tanımlanmış DTO sınıfları API üzerinden veri alışverişini sağlar.

```
/Controllers
  └── DeviceController.cs     # DeviceNameDto gibi controller-içi DTO sınıfları
```

## Veri Akışı

Bir API isteği şu şekilde işlenir:

1. İstek bir controller endpoint'ine gelir
2. Middleware ve filtreler isteği işler (authentication, validation, vb.)
3. Controller, gerekli iş mantığını uygular
4. DbContext üzerinden doğrudan veritabanı işlemleri yapılır
5. Sonuçlar ApiResponse formatında döndürülür

## Bağımlılık Enjeksiyonu (Dependency Injection)

Proje, .NET Core'un yerleşik DI (Dependency Injection) sistemini kullanır. Tüm servisler Program.cs (veya Startup.cs) dosyasında kaydedilir:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// DbContext kaydı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

// Controller'ların kaydı
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

// CORS politikası
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", 
        builder => builder.AllowAnyOrigin()
                         .AllowAnyMethod()
                         .AllowAnyHeader());
});

// JWT kimlik doğrulama
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        // JWT konfigürasyonu...
    });

var app = builder.Build();

// Middleware'lerin kaydı
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Veritabanı Erişimi

Proje, Entity Framework Core ORM'ini kullanarak veritabanı işlemlerini gerçekleştirir:

1. **Code-First Yaklaşımı**: Entity sınıfları önce tanımlanır, veritabanı bu sınıflardan otomatik oluşturulur.
2. **Migration**: Veritabanı şema değişiklikleri, EF Core Migration'ları ile yönetilir.
3. **Seeding**: Başlangıç verileri, Seeder sınıfları ile oluşturulur.

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

## Kimlik Doğrulama ve Yetkilendirme

Proje, JWT (JSON Web Token) tabanlı kimlik doğrulama kullanır:

1. Kullanıcılar, AuthController üzerinden giriş yapar ve bir token alır.
2. Sonraki isteklerde bu token, Authorization header'ında gönderilir.
3. Roller veya policy'ler kullanılarak endpoint erişimleri kontrol edilir.

## Validasyon

Model validasyonu iki seviyede gerçekleştirilir:

1. **Data Annotations**: Entity ve DTO sınıflarında validation attribute'ları kullanılır.
2. **ValidationFilter**: Utilities klasöründeki ValidationFilter sınıfı tüm istekler için merkezi validasyon sağlar.

## Hata Yönetimi

Hata yönetimi, aşağıdaki mekanizmalarla gerçekleştirilir:

1. **Try-Catch Blokları**: Kritik işlemler try-catch blokları içinde yapılır.
2. **ValidationFilter**: Validasyon hataları için merkezi hata yönetimi sağlar.
3. **ApiResponse**: Tüm hatalar standart ApiResponse formatında döndürülür. 