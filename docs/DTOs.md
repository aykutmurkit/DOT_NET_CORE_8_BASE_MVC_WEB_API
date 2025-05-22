# DTO (Data Transfer Object) Pattern

Data Transfer Object (DTO) pattern, veritabanı entity'leri ile API yanıtları arasında bir köprü görevi görür. Bu pattern, API üzerinden sadece gerekli verilerin paylaşılmasını sağlar ve veri taşıma süreçlerini optimize eder.

## Projede DTO Yapılanması

Projede DTO'lar iki farklı şekilde kullanılmaktadır:

1. **Özel Klasörde DTOs**: `/Models/DTOs/` klasörü altında, entity türlerine göre gruplandırılmış DTO sınıfları
2. **Controller İçi DTOs**: Bazı controller sınıfları içinde tanımlanmış özel amaçlı DTO sınıfları

```
/Models
  └── /DTOs
       ├── /Common              # Ortak kullanılan DTO sınıfları
       ├── /Device              # Cihaz ile ilgili DTO sınıfları
       │     ├── DeviceCreateDto.cs
       │     ├── DeviceReadDto.cs
       │     └── DeviceUpdateDto.cs
       └── /DeviceLog           # Cihaz log ile ilgili DTO sınıfları
             └── DeviceLogReadDto.cs

/Controllers
  └── DeviceController.cs       # DeviceNameDto gibi controller-içi DTO sınıfları
```

## DTO'ların Avantajları

1. **Veri Gizleme**: Entity'lerdeki hassas alanların API üzerinden gizlenmesini sağlar
2. **Esnek Yapı**: İstemci ihtiyaçlarına göre farklı veri yapıları oluşturabilirsiniz
3. **Performans**: Sadece gerekli verileri taşıyarak ağ trafiğini azaltır
4. **Veri Şekillendirme**: Entity yapısından bağımsız olarak veri şekillendirebilirsiniz
5. **Sürüm Yönetimi**: API sürümleri arasında geçiş yapılırken DTO'lar değiştirilebilir, entity'ler sabit kalabilir

## DTO Örnekleri

### Entity ve DTO Karşılaştırması

**Device Entity**:
```csharp
public class Device
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Cihaz adı zorunludur.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Cihaz adı 3-100 karakter arasında olmalıdır.")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Aktif durumu belirtilmelidir.")]
    public bool IsActive { get; set; }

    [Required(ErrorMessage = "Oluşturma tarihi zorunludur.")]
    public DateTime CreatedDate { get; set; }

    // One-to-many ilişki: Bir cihazın birden çok log kaydı olabilir
    public ICollection<DeviceLog> Logs { get; set; } = new List<DeviceLog>();
}
```

**DeviceReadDto**:
```csharp
public class DeviceReadDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class DeviceDetailDto : DeviceReadDto
{
    public List<Models.DTOs.DeviceLog.DeviceLogReadDto> Logs { get; set; } = new List<Models.DTOs.DeviceLog.DeviceLogReadDto>();
}
```

### İşlem Türüne Göre DTO'lar

**DeviceCreateDto** (Oluşturma İşlemi):
```csharp
public class DeviceCreateDto
{
    [Required(ErrorMessage = "Cihaz adı zorunludur.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Cihaz adı 3-100 karakter arasında olmalıdır.")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Aktif durumu belirtilmelidir.")]
    public bool IsActive { get; set; }
}
```

**DeviceUpdateDto** (Güncelleme İşlemi):
```csharp
public class DeviceUpdateDto
{
    [Required(ErrorMessage = "ID zorunludur.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Cihaz adı zorunludur.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Cihaz adı 3-100 karakter arasında olmalıdır.")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Aktif durumu belirtilmelidir.")]
    public bool IsActive { get; set; }
}
```

### Controller İçi DTO Örneği

DeviceController içinde tanımlanmış bir DTO sınıfı:

```csharp
/// <summary>
/// Cihaz ID ve isimlerini taşıyan DTO sınıfı
/// </summary>
public class DeviceNameDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

## Stored Procedure ile DTO Kullanımı

DTO'lar Stored Procedure sonuçlarını almak için de kullanılabilir:

```csharp
[HttpGet("names")]
public async Task<ActionResult<ApiResponse<List<DeviceNameDto>>>> GetDeviceNames()
{
    try
    {
        // Stored Procedure'ü doğrudan çağır
        var deviceNames = await _context.Database
            .SqlQueryRaw<DeviceNameDto>("EXEC GetDeviceNames")
            .ToListAsync();
        
        return ApiResponse<List<DeviceNameDto>>.Success(
            deviceNames, 
            $"{deviceNames.Count} adet aktif cihaz ismi listelendi."
        );
    }
    catch (Exception ex)
    {
        return this.ServerErrorResponse<List<DeviceNameDto>>(
            $"Cihaz isimleri alınırken bir hata oluştu: {ex.Message}"
        );
    }
}
```

## İlişkili Entity'leri DTO'lara Dönüştürme

İlişkili entity'leri de DTO'lara dönüştürerek nested yapılar oluşturabilirsiniz:

```csharp
// DeviceDetailDto içinde DeviceLogReadDto listesi var
public class DeviceDetailDto : DeviceReadDto
{
    public List<Models.DTOs.DeviceLog.DeviceLogReadDto> Logs { get; set; } = new List<Models.DTOs.DeviceLog.DeviceLogReadDto>();
}

// DeviceLogReadDto
public class DeviceLogReadDto
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string Message { get; set; }
    public string LogType { get; set; }
    public DateTime CreatedDate { get; set; }
    public int Severity { get; set; }
    public bool IsResolved { get; set; }
    public string ResolutionNotes { get; set; }
    public DateTime? ResolvedDate { get; set; }
}
```

## En İyi Uygulama Önerileri

1. **Organizasyon**: DTO'ları ilgili entity'lere göre klasörlerde gruplayın
2. **Adlandırma Kuralları**: 
   - DTO tipini belirtin: `DeviceReadDto`, `DeviceCreateDto`
   - Controller içi DTO'larda amaç belirtin: `DeviceNameDto`
3. **Validasyon**: Validasyon kurallarını DTO'larda tanımlayın, entity'lerde değil
4. **Kalıtım**: Ortak özellikleri paylaşan DTO'lar için kalıtım kullanın
5. **İhtiyaca Göre DTO**: Her endpoint için sadece gerekli özellikleri içeren DTO'lar tasarlayın
6. **İlişkilerin Yönetimi**: İlişkili entity'leri taşırken ID ve özet bilgilerle sınırlandırın 