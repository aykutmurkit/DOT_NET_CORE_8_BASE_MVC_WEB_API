using Base.Data.Context;
using Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Base.Data.Seeding
{
    /// <summary>
    /// DeviceLog entity için seed data
    /// </summary>
    public class DeviceLogSeeder : ISeeder
    {
        private static readonly ILogger<DeviceLogSeeder> _logger = 
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<DeviceLogSeeder>();
            
        /// <summary>
        /// Device seed'inden sonra çalışmalı
        /// </summary>
        public int Order => 2;

        public async Task SeedAsync(AppDbContext context)
        {
            _logger.LogInformation("DeviceLogSeeder başlatılıyor...");
            
            // DeviceLogs tablosunda veri varsa işlem yapma
            if (await context.DeviceLogs.AnyAsync())
            {
                _logger.LogInformation("DeviceLogs tablosunda veri var, işlem yapılmıyor.");
                return;
            }

            // Cihazların olduğundan emin ol
            if (!await context.Devices.AnyAsync())
            {
                _logger.LogWarning("Devices tablosunda veri bulunamadı. DeviceLogSeeder çalışmıyor.");
                return;
            }

            try
            {
                _logger.LogInformation("Entity Framework ile veri ekleme deneniyor...");
                
                // EF Core ile doğrudan ekleme yap
                var logs = new List<DeviceLog>
                {
                    new DeviceLog { Id = 1, DeviceId = 1, Message = "Cihaz başarıyla açıldı", LogType = "Info", CreatedDate = DateTime.Now.AddDays(-5), Severity = 1, IsResolved = true, ResolutionNotes = "Sorun yoktu", ResolvedDate = DateTime.Now.AddDays(-5) },
                    new DeviceLog { Id = 2, DeviceId = 1, Message = "Sıcaklık yükseldi", LogType = "Warning", CreatedDate = DateTime.Now.AddDays(-3), Severity = 2, IsResolved = true, ResolutionNotes = "Soğutma sistemi çalıştırıldı", ResolvedDate = DateTime.Now.AddDays(-3).AddHours(2) },
                    new DeviceLog { Id = 3, DeviceId = 2, Message = "Bağlantı hatası", LogType = "Error", CreatedDate = DateTime.Now.AddDays(-4), Severity = 4, IsResolved = true, ResolutionNotes = "Ağ kablosu değiştirildi", ResolvedDate = DateTime.Now.AddDays(-3) },
                    new DeviceLog { Id = 4, DeviceId = 3, Message = "Pil seviyesi düşük", LogType = "Warning", CreatedDate = DateTime.Now.AddDays(-2), Severity = 3, IsResolved = false, ResolutionNotes = "", ResolvedDate = null },
                    new DeviceLog { Id = 5, DeviceId = 4, Message = "Hareket algılandı", LogType = "Info", CreatedDate = DateTime.Now.AddDays(-1), Severity = 1, IsResolved = true, ResolutionNotes = "Normal hareket", ResolvedDate = DateTime.Now.AddDays(-1).AddMinutes(30) },
                    new DeviceLog { Id = 6, DeviceId = 5, Message = "Firmware güncellendi", LogType = "Info", CreatedDate = DateTime.Now.AddHours(-12), Severity = 1, IsResolved = true, ResolutionNotes = "Başarılı güncelleme", ResolvedDate = DateTime.Now.AddHours(-11) },
                    new DeviceLog { Id = 7, DeviceId = 2, Message = "Sensör hatası", LogType = "Error", CreatedDate = DateTime.Now.AddHours(-8), Severity = 5, IsResolved = false, ResolutionNotes = "", ResolvedDate = null },
                    new DeviceLog { Id = 8, DeviceId = 1, Message = "Rutin bakım tamamlandı", LogType = "Info", CreatedDate = DateTime.Now.AddHours(-5), Severity = 1, IsResolved = true, ResolutionNotes = "Tüm kontroller yapıldı", ResolvedDate = DateTime.Now.AddHours(-4) }
                };

                // Identity Insert açık
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [DeviceLogs] ON;");
                
                // Verileri ekle
                await context.DeviceLogs.AddRangeAsync(logs);
                await context.SaveChangesAsync();
                
                // Identity Insert kapalı
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [DeviceLogs] OFF;");
                
                _logger.LogInformation("DeviceLog verileri başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entity Framework ile veri eklerken hata: {Message}", ex.Message);
                
                try
                {
                    _logger.LogInformation("SQL komutları ile alternatif veri ekleme deneniyor...");
                    
                    // Tüm logları tek seferde ekle
                    var queryBuilder = new StringBuilder();
                    queryBuilder.AppendLine("SET IDENTITY_INSERT [DeviceLogs] ON;");
                    queryBuilder.AppendLine("INSERT INTO [DeviceLogs] ([Id], [DeviceId], [Message], [LogType], [CreatedDate], [Severity], [IsResolved], [ResolutionNotes], [ResolvedDate]) VALUES");

                    // Log bilgileri - NULL değerler yerine boş string kullanılıyor
                    var logs = new List<(int id, int deviceId, string message, string logType, DateTime createdDate, int severity, bool isResolved, string resolutionNotes, DateTime? resolvedDate)>
                    {
                        (1, 1, "Cihaz başarıyla açıldı", "Info", DateTime.Now.AddDays(-5), 1, true, "Sorun yoktu", DateTime.Now.AddDays(-5)),
                        (2, 1, "Sıcaklık yükseldi", "Warning", DateTime.Now.AddDays(-3), 2, true, "Soğutma sistemi çalıştırıldı", DateTime.Now.AddDays(-3).AddHours(2)),
                        (3, 2, "Bağlantı hatası", "Error", DateTime.Now.AddDays(-4), 4, true, "Ağ kablosu değiştirildi", DateTime.Now.AddDays(-3)),
                        (4, 3, "Pil seviyesi düşük", "Warning", DateTime.Now.AddDays(-2), 3, false, "", null), // NULL yerine boş string
                        (5, 4, "Hareket algılandı", "Info", DateTime.Now.AddDays(-1), 1, true, "Normal hareket", DateTime.Now.AddDays(-1).AddMinutes(30)),
                        (6, 5, "Firmware güncellendi", "Info", DateTime.Now.AddHours(-12), 1, true, "Başarılı güncelleme", DateTime.Now.AddHours(-11)),
                        (7, 2, "Sensör hatası", "Error", DateTime.Now.AddHours(-8), 5, false, "", null), // NULL yerine boş string
                        (8, 1, "Rutin bakım tamamlandı", "Info", DateTime.Now.AddHours(-5), 1, true, "Tüm kontroller yapıldı", DateTime.Now.AddHours(-4))
                    };

                    // SQL komutunu oluştur
                    for (int i = 0; i < logs.Count; i++)
                    {
                        var log = logs[i];
                        string createdDate = log.createdDate.ToString("yyyy-MM-dd HH:mm:ss");
                        string isResolved = log.isResolved ? "1" : "0";
                        string resolvedDate = log.resolvedDate.HasValue ? $"'{log.resolvedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}'" : "NULL";
                        string resolutionNotes = $"'{log.resolutionNotes}'"; // Artık tüm değerler string, NULL değil

                        queryBuilder.Append($"({log.id}, {log.deviceId}, '{log.message}', '{log.logType}', '{createdDate}', {log.severity}, {isResolved}, {resolutionNotes}, {resolvedDate})");
                        
                        if (i < logs.Count - 1)
                            queryBuilder.AppendLine(",");
                        else
                            queryBuilder.AppendLine(";");
                    }

                    queryBuilder.AppendLine("SET IDENTITY_INSERT [DeviceLogs] OFF;");

                    string sqlCommand = queryBuilder.ToString();
                    _logger.LogInformation("Çalıştırılacak SQL komutu: {SqlCommand}", sqlCommand);
                    
                    // SQL komutunu çalıştır
                    await context.Database.ExecuteSqlRawAsync(sqlCommand);
                    _logger.LogInformation("SQL komutları ile veri ekleme başarılı.");
                }
                catch (Exception sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL komutları ile veri eklerken hata: {Message}", sqlEx.Message);
                    throw new Exception($"DeviceLog seed işlemi başarısız oldu: {sqlEx.Message}", sqlEx);
                }
            }

            // Context cache'ini yenile
            foreach (var entry in context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }
    }
} 