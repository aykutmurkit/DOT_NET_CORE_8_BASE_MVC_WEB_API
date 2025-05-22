using Base.Data.Context;
using Base.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Base.Data.Seeding
{
    /// <summary>
    /// Device entity için seed data
    /// </summary>
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

            // SQL komutunu oluştur
            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                string createdDate = device.createdDate.ToString("yyyy-MM-dd HH:mm:ss");
                string isActive = device.isActive ? "1" : "0";

                queryBuilder.Append($"({device.id}, '{device.name}', '{device.description}', {isActive}, '{createdDate}')");
                
                if (i < devices.Count - 1)
                    queryBuilder.AppendLine(",");
                else
                    queryBuilder.AppendLine(";");
            }

            queryBuilder.AppendLine("SET IDENTITY_INSERT [Devices] OFF;");

            // SQL komutunu çalıştır
            await context.Database.ExecuteSqlRawAsync(queryBuilder.ToString());
            
            // Context cache'ini yenile
            foreach (var entry in context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
            
            // Stored Procedure'leri oluştur
            await CreateStoredProceduresAsync(context);
        }
        
        /// <summary>
        /// Cihazlarla ilgili Stored Procedure'leri oluşturur
        /// </summary>
        private async Task CreateStoredProceduresAsync(AppDbContext context)
        {
            // Önce SP varsa sil (idempotent olması için)
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetDeviceNames')
            BEGIN
                DROP PROCEDURE GetDeviceNames
            END";
            
            await context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
            // Cihaz isimlerini getiren SP'yi oluştur
            var createSPQuery = @"
            CREATE PROCEDURE GetDeviceNames
            AS
            BEGIN
                SET NOCOUNT ON;
                
                SELECT Id, Name 
                FROM Devices
                WHERE IsActive = 1
                ORDER BY Name ASC
            END";
            
            await context.Database.ExecuteSqlRawAsync(createSPQuery);
        }
    }
} 