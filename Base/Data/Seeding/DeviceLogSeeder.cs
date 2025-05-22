using Base.Data.Context;
using Base.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Base.Data.Seeding
{
    /// <summary>
    /// DeviceLog entity için seed data
    /// </summary>
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
            var queryBuilder = new StringBuilder();
            queryBuilder.AppendLine("SET IDENTITY_INSERT [DeviceLogs] ON;");
            queryBuilder.AppendLine("INSERT INTO [DeviceLogs] ([Id], [DeviceId], [Message], [LogType], [CreatedDate], [Severity], [IsResolved], [ResolutionNotes], [ResolvedDate]) VALUES");

            // Log bilgileri
            var logs = new List<(int id, int deviceId, string message, string logType, DateTime createdDate, int severity, bool isResolved, string resolutionNotes, DateTime? resolvedDate)>
            {
                (1, 1, "Cihaz başarıyla açıldı", "Info", DateTime.Now.AddDays(-5), 1, true, "Sorun yoktu", DateTime.Now.AddDays(-5)),
                (2, 1, "Sıcaklık yükseldi", "Warning", DateTime.Now.AddDays(-3), 2, true, "Soğutma sistemi çalıştırıldı", DateTime.Now.AddDays(-3).AddHours(2)),
                (3, 2, "Bağlantı hatası", "Error", DateTime.Now.AddDays(-4), 4, true, "Ağ kablosu değiştirildi", DateTime.Now.AddDays(-3)),
                (4, 3, "Pil seviyesi düşük", "Warning", DateTime.Now.AddDays(-2), 3, false, null, null),
                (5, 4, "Hareket algılandı", "Info", DateTime.Now.AddDays(-1), 1, true, "Normal hareket", DateTime.Now.AddDays(-1).AddMinutes(30)),
                (6, 5, "Firmware güncellendi", "Info", DateTime.Now.AddHours(-12), 1, true, "Başarılı güncelleme", DateTime.Now.AddHours(-11)),
                (7, 2, "Sensör hatası", "Error", DateTime.Now.AddHours(-8), 5, false, null, null),
                (8, 1, "Rutin bakım tamamlandı", "Info", DateTime.Now.AddHours(-5), 1, true, "Tüm kontroller yapıldı", DateTime.Now.AddHours(-4))
            };

            // SQL komutunu oluştur
            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                string createdDate = log.createdDate.ToString("yyyy-MM-dd HH:mm:ss");
                string isResolved = log.isResolved ? "1" : "0";
                string resolvedDate = log.resolvedDate.HasValue ? $"'{log.resolvedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}'" : "NULL";
                string resolutionNotes = log.resolutionNotes != null ? $"'{log.resolutionNotes}'" : "NULL";

                queryBuilder.Append($"({log.id}, {log.deviceId}, '{log.message}', '{log.logType}', '{createdDate}', {log.severity}, {isResolved}, {resolutionNotes}, {resolvedDate})");
                
                if (i < logs.Count - 1)
                    queryBuilder.AppendLine(",");
                else
                    queryBuilder.AppendLine(";");
            }

            queryBuilder.AppendLine("SET IDENTITY_INSERT [DeviceLogs] OFF;");

            // SQL komutunu çalıştır
            await context.Database.ExecuteSqlRawAsync(queryBuilder.ToString());
            
            // Context cache'ini yenile
            foreach (var entry in context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }
    }
} 