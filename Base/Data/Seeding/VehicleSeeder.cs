using Base.Data.Context;
using Base.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Base.Data.Seeding
{
    /// <summary>
    /// Vehicle entity için seed data
    /// </summary>
    public class VehicleSeeder : ISeeder
    {
        private static readonly ILogger<VehicleSeeder> _logger = 
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<VehicleSeeder>();

        /// <summary>
        /// Seed etme sırası
        /// </summary>
        public int Order => 3;

        public async Task SeedAsync(AppDbContext context)
        {
            _logger.LogInformation("VehicleSeeder başlatılıyor...");
            
            // Vehicles tablosunda veri varsa işlem yapma
            if (await context.Vehicles.AnyAsync())
            {
                _logger.LogInformation("Vehicles tablosunda veri var, sadece SP'ler oluşturuluyor.");
                // Veri varsa, Stored Procedure'leri oluştur
                await CreateStoredProceduresAsync(context);
                return;
            }

            try
            {
                _logger.LogInformation("SQL komutları ile veri ekleme deneniyor...");
                
                // Tüm araçları SQL ile ekle
                var queryBuilder = new StringBuilder();
                queryBuilder.AppendLine("SET IDENTITY_INSERT [Vehicles] ON;");
                queryBuilder.AppendLine("INSERT INTO [Vehicles] ([Id], [Name], [CreatedDate], [UpdatedDate]) VALUES");
                
                // Sadece 5 temel veri ile başla
                queryBuilder.AppendLine("(1, 'Sedan', GETDATE(), NULL),");
                queryBuilder.AppendLine("(2, 'SUV', GETDATE(), GETDATE()),");
                queryBuilder.AppendLine("(3, 'Hatchback', GETDATE(), NULL),");
                queryBuilder.AppendLine("(4, 'Pickup', GETDATE(), GETDATE()),");
                queryBuilder.AppendLine("(5, 'Minivan', GETDATE(), NULL);");
                
                queryBuilder.AppendLine("SET IDENTITY_INSERT [Vehicles] OFF;");
                
                string sqlCommand = queryBuilder.ToString();
                _logger.LogInformation("Çalıştırılacak SQL komutu: {SqlCommand}", sqlCommand);
                
                // SQL komutunu çalıştır
                await context.Database.ExecuteSqlRawAsync(sqlCommand);
                _logger.LogInformation("SQL komutları ile veri ekleme başarılı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL komutları ile veri eklerken hata: {Message}", ex.Message);
                throw new Exception($"Vehicle seed işlemi başarısız oldu: {ex.Message}", ex);
            }
            
            // Context cache'ini yenile
            foreach (var entry in context.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
            
            // Stored Procedure'leri oluştur
            await CreateStoredProceduresAsync(context);
        }
        
        /// <summary>
        /// Araçlarla ilgili Stored Procedure'leri oluşturur
        /// </summary>
        private async Task CreateStoredProceduresAsync(AppDbContext context)
        {
            try
            {
                _logger.LogInformation("Stored Procedure'ler oluşturuluyor...");
                
                // Tüm araçları getiren SP
                await CreateGetAllVehiclesSP(context);
                
                // Id'ye göre araç getiren SP
                await CreateGetVehicleByIdSP(context);
                
                // Yeni araç ekleyen SP
                await CreateAddVehicleSP(context);
                
                // Araç güncelleyen SP
                await CreateUpdateVehicleSP(context);
                
                // Araç silen SP
                await CreateDeleteVehicleSP(context);
                
                _logger.LogInformation("Stored Procedure'ler başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stored Procedure oluşturma hatası: {Message}", ex.Message);
                throw new Exception($"Vehicle stored procedure oluşturma işlemi sırasında hata oluştu: {ex.Message}", ex);
            }
        }
        
        private async Task CreateGetAllVehiclesSP(AppDbContext context)
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetAllVehicles')
            BEGIN
                DROP PROCEDURE GetAllVehicles
            END";
            
            await context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
            // Tüm araçları getiren SP'yi oluştur
            var createSPQuery = @"
            CREATE PROCEDURE GetAllVehicles
            AS
            BEGIN
                SET NOCOUNT ON;
                
                SELECT Id, Name, CreatedDate, UpdatedDate 
                FROM Vehicles
                ORDER BY Name ASC
            END";
            
            await context.Database.ExecuteSqlRawAsync(createSPQuery);
        }
        
        private async Task CreateGetVehicleByIdSP(AppDbContext context)
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetVehicleById')
            BEGIN
                DROP PROCEDURE GetVehicleById
            END";
            
            await context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
            // ID'ye göre araç getiren SP'yi oluştur
            var createSPQuery = @"
            CREATE PROCEDURE GetVehicleById
                @Id int
            AS
            BEGIN
                SET NOCOUNT ON;
                
                SELECT Id, Name, CreatedDate, UpdatedDate 
                FROM Vehicles
                WHERE Id = @Id
            END";
            
            await context.Database.ExecuteSqlRawAsync(createSPQuery);
        }
        
        private async Task CreateAddVehicleSP(AppDbContext context)
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'AddVehicle')
            BEGIN
                DROP PROCEDURE AddVehicle
            END";
            
            await context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
            // Yeni araç ekleyen SP'yi oluştur
            var createSPQuery = @"
            CREATE PROCEDURE AddVehicle
                @Name nvarchar(100),
                @CreatedDate datetime
            AS
            BEGIN
                SET NOCOUNT ON;
                
                INSERT INTO Vehicles (Name, CreatedDate)
                VALUES (@Name, @CreatedDate);
                
                SELECT SCOPE_IDENTITY() as Id;
            END";
            
            await context.Database.ExecuteSqlRawAsync(createSPQuery);
        }
        
        private async Task CreateUpdateVehicleSP(AppDbContext context)
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'UpdateVehicle')
            BEGIN
                DROP PROCEDURE UpdateVehicle
            END";
            
            await context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
            // Araç güncelleyen SP'yi oluştur
            var createSPQuery = @"
            CREATE PROCEDURE UpdateVehicle
                @Id int,
                @Name nvarchar(100),
                @UpdatedDate datetime
            AS
            BEGIN
                SET NOCOUNT ON;
                
                UPDATE Vehicles
                SET Name = @Name,
                    UpdatedDate = @UpdatedDate
                WHERE Id = @Id;
                
                SELECT @@ROWCOUNT as AffectedRows;
            END";
            
            await context.Database.ExecuteSqlRawAsync(createSPQuery);
        }
        
        private async Task CreateDeleteVehicleSP(AppDbContext context)
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'DeleteVehicle')
            BEGIN
                DROP PROCEDURE DeleteVehicle
            END";
            
            await context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
            // Araç silen SP'yi oluştur
            var createSPQuery = @"
            CREATE PROCEDURE DeleteVehicle
                @Id int
            AS
            BEGIN
                SET NOCOUNT ON;
                
                DELETE FROM Vehicles
                WHERE Id = @Id;
                
                SELECT @@ROWCOUNT as AffectedRows;
            END";
            
            await context.Database.ExecuteSqlRawAsync(createSPQuery);
        }
    }
} 