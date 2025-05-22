# Veritabanı Seed İşlemi

Seed işlemi, veritabanında başlangıç verilerinin oluşturulması işlemidir. Bu işlem, uygulamanın test edilmesi, demo verilerinin oluşturulması veya gerekli referans verilerinin eklenmesi için kullanılır.

## Projede Seed Yapısı

Projede seed işlemleri, hiyerarşik bir yapıda organize edilmiştir:

```
/Data
  └── /Seeding
        ├── ISeeder.cs          # Tüm seeder'lar için arayüz
        ├── DatabaseSeeder.cs   # Seeder'ları bulan ve yöneten ana sınıf
        ├── DeviceSeeder.cs     # Cihaz verilerini oluşturma (Order: 1)
        └── DeviceLogSeeder.cs  # Cihaz log verilerini oluşturma (Order: 2)
```

## ISeeder Arayüzü

Tüm seeder sınıfları `ISeeder` arayüzünü implemente eder:

```csharp
/// <summary>
/// Seed data işlemleri için arayüz
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Seed etme sirasini belirler. Dusuk sayilar once calisir.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Seed islemini gerceklestirir
    /// </summary>
    /// <param name="context">Veritabani baglanti context'i</param>
    Task SeedAsync(AppDbContext context);
}
```

## DatabaseSeeder - Merkezi Yönetim

`DatabaseSeeder` sınıfı, tüm seeder'ları bulan ve sırasıyla çalıştıran merkezi yönetim sınıfıdır:

```csharp
public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Tüm seed işlemlerini gerçekleştirir
    /// </summary>
    public async Task SeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ISeeder interface'ini implement eden tüm sınıfları bul
        var seeders = GetSeeders();

        foreach (var seeder in seeders.OrderBy(s => s.Order))
        {
            try
            {
                _logger.LogInformation("Seeding: {SeederName} başlatılıyor...", seeder.GetType().Name);
                await seeder.SeedAsync(context);
                _logger.LogInformation("Seeding: {SeederName} başarıyla tamamlandı.", seeder.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seeding: {SeederName} sırasında hata oluştu: {ErrorMessage}", 
                    seeder.GetType().Name, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Uygulamadaki tüm ISeeder implementasyonlarını bulur ve örneklerini oluşturur
    /// </summary>
    private List<ISeeder> GetSeeders()
    {
        var seeders = new List<ISeeder>();
        
        // ISeeder interface'ini implement eden tüm tipleri bul
        var seederTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && typeof(ISeeder).IsAssignableFrom(t));

        // Her tip için bir örnek oluştur
        foreach (var seederType in seederTypes)
        {
            var seeder = Activator.CreateInstance(seederType) as ISeeder;
            if (seeder != null)
            {
                seeders.Add(seeder);
            }
        }

        return seeders;
    }
}
```

## DeviceSeeder Örneği

`DeviceSeeder` sınıfı, Devices tablosuna örnek veri ekler ve ilgili Stored Procedure'ları oluşturur:

```csharp
public class DeviceSeeder : ISeeder
{
    /// <summary>
    /// Seed etme sırası
    /// </summary>
    public int Order => 1;

    public async Task SeedAsync(AppDbContext context)
    {
        // Devices tablosunda veri varsa işlem yapma
        if (await context.Devices.AnyAsync())
        {
            // Veri varsa, Stored Procedure'ü oluştur
            await CreateStoredProceduresAsync(context);
            return;
        }

        // Tüm cihazları tek seferde ekle
        var queryBuilder = new StringBuilder();
        queryBuilder.AppendLine("SET IDENTITY_INSERT [Devices] ON;");
        queryBuilder.AppendLine("INSERT INTO [Devices] ([Id], [Name], [Description], [IsActive], [CreatedDate]) VALUES");

        // Cihaz bilgileri
        var devices = new List<(int id, string name, string description, bool isActive, DateTime createdDate)>
        {
            (1, "Akıllı Termostat", "Ev sıcaklığını uzaktan kontrol etmek için akıllı termostat", true, DateTime.Now.AddDays(-10)),
            (2, "Güvenlik Kamerası", "Yüksek çözünürlüklü güvenlik kamerası", true, DateTime.Now.AddDays(-8)),
            (3, "Akıllı Kapı Kilidi", "Uzaktan kontrol edilebilen kapı kilidi", true, DateTime.Now.AddDays(-5)),
            (4, "Hareket Sensörü", "Evdeki hareketleri algılayan sensör", false, DateTime.Now.AddDays(-3)),
            (5, "Akıllı Işık", "Uzaktan kontrol edilebilen akıllı ışık", true, DateTime.Now.AddDays(-1))
        };

        // SQL komutunu oluştur ve çalıştır
        // ... (SQL komut oluşturma kodu)
        
        // Stored Procedure'leri oluştur
        await CreateStoredProceduresAsync(context);
    }
    
    /// <summary>
    /// Cihazlarla ilgili Stored Procedure'leri oluşturur
    /// </summary>
    private async Task CreateStoredProceduresAsync(AppDbContext context)
    {
        // GetDeviceNames stored procedure oluştur
        // ... (SQL komut oluşturma kodu)
    }
}
```

## DeviceLogSeeder Örneği

`DeviceLogSeeder` sınıfı, DeviceLogs tablosuna örnek log kayıtları ekler:

```csharp
public class DeviceLogSeeder : ISeeder
{
    /// <summary>
    /// Device seed'inden sonra çalışmalı
    /// </summary>
    public int Order => 2;

    public async Task SeedAsync(AppDbContext context)
    {
        // DeviceLogs tablosunda veri varsa işlem yapma
        if (await context.DeviceLogs.AnyAsync())
        {
            return;
        }

        // Cihazların olduğundan emin ol
        if (!await context.Devices.AnyAsync())
        {
            return;
        }

        // Tüm logları tek seferde ekle
        // ... (SQL komut oluşturma kodu)
    }
}
```

## Program.cs İçindeki Kullanım

`Program.cs` içinde DatabaseSeeder servis olarak kaydedilir ve uygulama başlangıcında çalıştırılır:

```csharp
// Add database seeder
builder.Services.AddTransient<DatabaseSeeder>();

// ...

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Veritabanı yapılandırması
        // ...
        
        // Apply migrations
        logger.LogInformation("Veritabanı ve tablolar oluşturuluyor...");
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Veritabanı ve tablolar oluşturuldu.");
        
        // Seed data - konfigürasyona göre koşullu olarak çalıştır
        var enableSeeding = app.Configuration.GetValue<bool>("Database:Seed:EnableSeeding");
        if (enableSeeding)
        {
            logger.LogInformation("Seed işlemi başlatılıyor...");
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("Seed işlemi tamamlandı.");
        }
        else
        {
            logger.LogInformation("Seed işlemi yapılandırma dosyasında devre dışı bırakılmış.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı işlemlerinde bir hata oluştu.");
    }
}
```

## Performans Optimizasyonu

Projedeki seeder'lar, `AddRangeAsync` ve ardından `SaveChangesAsync` gibi EF Core metodları yerine doğrudan SQL komutları kullanarak büyük veri setlerini veritabanına ekler. Bu yaklaşım özellikle büyük veri setleri için performans avantajı sağlar.

```csharp
// Tüm cihazları tek seferde ekle
var queryBuilder = new StringBuilder();
queryBuilder.AppendLine("SET IDENTITY_INSERT [Devices] ON;");
queryBuilder.AppendLine("INSERT INTO [Devices] ([Id], [Name], [Description], [IsActive], [CreatedDate]) VALUES");

// ... (Veri ekleme kodu)

// SQL komutunu çalıştır
await context.Database.ExecuteSqlRawAsync(queryBuilder.ToString());
```

## Seed İşlemini Yapılandırma

Projedeki seed işlemi, appsettings.json dosyasındaki yapılandırma ayarları ile kontrol edilir:

```json
"Database": {
  "Seed": {
    "EnableSeeding": true
  }
}
```

Bu yapılandırma, farklı ortamlarda (development, staging, production) seed işleminin davranışını kontrol etmek için kullanılabilir.

## En İyi Uygulama Önerileri

1. **Sıralı Çalıştırma**: `Order` özelliği ile seed işlemlerinin doğru sırada çalışmasını sağlayın
2. **İdempotent Tasarım**: Seed metodları, birden fazla kez çalıştırılabilir olmalı
3. **Reflection ile Seeder Bulma**: `GetSeeders` gibi dinamik seeder bulma metodları kullanın
4. **SQL İle Toplu Ekleme**: Büyük veri setleri için EF Core yerine doğrudan SQL komutları kullanın
5. **Stored Procedure Oluşturma**: Seed işlemi sırasında veritabanı nesnelerini de oluşturun
6. **Yapılandırılabilir Seeding**: Farklı ortamlarda farklı seed davranışları için yapılandırma kullanın 