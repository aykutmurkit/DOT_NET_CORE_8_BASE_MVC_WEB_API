using Base.Data.Context;
using Base.Models;
using Base.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Base.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Vehicle>>>> GetAll()
        {
            try
            {
                // Stored Procedure yerine doğrudan Entity Framework kullan
                var vehicles = await _context.Vehicles
                    .OrderBy(v => v.Name)
                    .ToListAsync();
                
                return ApiResponse<List<Vehicle>>.Success(vehicles, "Araçlar başarıyla listelendi.");
            }
            catch (Exception ex)
            {
                return this.ServerErrorResponse<List<Vehicle>>(
                    $"Araçlar listelenirken bir hata oluştu: {ex.Message}"
                );
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Vehicle>>> GetById(int id)
        {
            try
            {
                // Stored Procedure'ü çağır
                var parameters = new []
                {
                    new SqlParameter("@Id", SqlDbType.Int) { Value = id }
                };
                
                var vehicles = await _context.Database
                    .SqlQueryRaw<Vehicle>("EXEC GetVehicleById @Id", parameters)
                    .ToListAsync();
                
                if (vehicles == null || vehicles.Count == 0)
                {
                    return this.NotFoundResponse<Vehicle>("Araç bulunamadı.");
                }
                
                var vehicle = vehicles[0];
                return ApiResponse<Vehicle>.Success(vehicle, "Araç başarıyla getirildi.");
            }
            catch (Exception ex)
            {
                return this.ServerErrorResponse<Vehicle>(
                    $"Araç getirilirken bir hata oluştu: {ex.Message}"
                );
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<Vehicle>>> Create(Vehicle vehicle)
        {
            try
            {
                // Oluşturma tarihini ayarla
                vehicle.CreatedDate = DateTime.Now;
                
                // Stored Procedure'ü çağır
                var parameters = new []
                {
                    new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = vehicle.Name },
                    new SqlParameter("@CreatedDate", SqlDbType.DateTime) { Value = vehicle.CreatedDate }
                };
                
                // SP, eklenen kaydın ID'sini döner - önce ToList() ile sonuçları alıp sonra FirstOrDefault kullanmalıyız
                var result = await _context.Database
                    .SqlQueryRaw<decimal>("EXEC AddVehicle @Name, @CreatedDate", parameters)
                    .ToListAsync();
                
                if (result != null && result.Count > 0)
                {
                    vehicle.Id = Convert.ToInt32(result[0]); // Decimal'i Int32'ye dönüştür
                    
                    // Eklenen aracı getir
                    var parameters2 = new[]
                    {
                        new SqlParameter("@Id", SqlDbType.Int) { Value = vehicle.Id }
                    };
                    
                    var addedVehicle = await _context.Database
                        .SqlQueryRaw<Vehicle>("EXEC GetVehicleById @Id", parameters2)
                        .ToListAsync();
                        
                    if (addedVehicle != null && addedVehicle.Count > 0)
                    {
                        return this.CreatedResponse(addedVehicle[0], "Araç başarıyla oluşturuldu.");
                    }
                    
                    return this.CreatedResponse(vehicle, "Araç başarıyla oluşturuldu.");
                }
                
                return this.ServerErrorResponse<Vehicle>("Araç oluşturulurken bir hata oluştu: ID alınamadı.");
            }
            catch (Exception ex)
            {
                return this.ServerErrorResponse<Vehicle>(
                    $"Araç oluşturulurken bir hata oluştu: {ex.Message}"
                );
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<ApiResponse<Vehicle>>> Update(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
            {
                return this.BadRequestResponse<Vehicle>("ID eşleşmiyor.");
            }

            try
            {
                // Güncelleme tarihini ayarla
                vehicle.UpdatedDate = DateTime.Now;
                
                // Stored Procedure'ü çağır
                var parameters = new []
                {
                    new SqlParameter("@Id", SqlDbType.Int) { Value = id },
                    new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = vehicle.Name },
                    new SqlParameter("@UpdatedDate", SqlDbType.DateTime) { Value = vehicle.UpdatedDate }
                };
                
                // SP, etkilenen satır sayısını döner
                var result = await _context.Database
                    .SqlQueryRaw<int>("EXEC UpdateVehicle @Id, @Name, @UpdatedDate", parameters)
                    .ToListAsync();
                
                int affectedRows = (result != null && result.Count > 0) ? result[0] : 0;
                
                if (affectedRows == 0)
                {
                    return this.NotFoundResponse<Vehicle>("Güncellenecek araç bulunamadı.");
                }
                
                return ApiResponse<Vehicle>.Success(vehicle, "Araç başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return this.ServerErrorResponse<Vehicle>(
                    $"Araç güncellenirken bir hata oluştu: {ex.Message}"
                );
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireSuperAdminRole")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            try
            {
                // Stored Procedure'ü çağır
                var parameters = new []
                {
                    new SqlParameter("@Id", SqlDbType.Int) { Value = id }
                };
                
                // SP, etkilenen satır sayısını döner
                var result = await _context.Database
                    .SqlQueryRaw<int>("EXEC DeleteVehicle @Id", parameters)
                    .ToListAsync();
                
                int affectedRows = (result != null && result.Count > 0) ? result[0] : 0;
                
                if (affectedRows == 0)
                {
                    return this.NotFoundResponse<object>("Silinecek araç bulunamadı.");
                }
                
                return this.NoContentResponse("Araç başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return this.ServerErrorResponse<object>(
                    $"Araç silinirken bir hata oluştu: {ex.Message}"
                );
            }
        }
        
        /// <summary>
        /// API test örnekleri için endpointler ve JSON örneklerini döndürür
        /// </summary>
        [HttpGet("samples")]
        public ActionResult<ApiResponse<object>> GetSamples()
        {
            var samples = new
            {
                GetAllVehicles = new
                {
                    Endpoint = "/api/vehicle",
                    Method = "GET",
                    Description = "Tüm araçları listeler"
                },
                GetVehicleById = new
                {
                    Endpoint = "/api/vehicle/1",
                    Method = "GET",
                    Description = "ID'ye göre araç getirir"
                },
                CreateVehicle = new
                {
                    Endpoint = "/api/vehicle",
                    Method = "POST",
                    Description = "Yeni bir araç oluşturur (Admin rolü gerektirir)",
                    SampleRequest = new
                    {
                        name = "Coupe"
                    }
                },
                UpdateVehicle = new
                {
                    Endpoint = "/api/vehicle/1",
                    Method = "PUT",
                    Description = "Mevcut bir aracı günceller (Admin rolü gerektirir)",
                    SampleRequest = new
                    {
                        id = 1,
                        name = "Sedan Sport"
                    }
                },
                DeleteVehicle = new
                {
                    Endpoint = "/api/vehicle/5",
                    Method = "DELETE",
                    Description = "Bir aracı siler (SuperAdmin rolü gerektirir)"
                }
            };
            
            return ApiResponse<object>.Success(samples, "Vehicle API test örnekleri");
        }

        [HttpGet("seed-sp")]
        public async Task<ActionResult<ApiResponse<string>>> SeedStoredProcedures()
        {
            try
            {
                // GetAllVehicles SP
                await CreateGetAllVehiclesSP();
                
                // GetVehicleById SP
                await CreateGetVehicleByIdSP();
                
                // AddVehicle SP
                await CreateAddVehicleSP();
                
                // UpdateVehicle SP
                await CreateUpdateVehicleSP();
                
                // DeleteVehicle SP
                await CreateDeleteVehicleSP();
                
                return ApiResponse<string>.Success("Stored procedure'ler başarıyla oluşturuldu.", "İşlem başarılı");
            }
            catch (Exception ex)
            {
                return this.ServerErrorResponse<string>(
                    $"Stored procedure'ler oluşturulurken bir hata oluştu: {ex.Message}"
                );
            }
        }

        private async Task CreateGetAllVehiclesSP()
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetAllVehicles')
            BEGIN
                DROP PROCEDURE GetAllVehicles
            END";
            
            await _context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
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
            
            await _context.Database.ExecuteSqlRawAsync(createSPQuery);
        }

        private async Task CreateGetVehicleByIdSP()
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetVehicleById')
            BEGIN
                DROP PROCEDURE GetVehicleById
            END";
            
            await _context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
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
            
            await _context.Database.ExecuteSqlRawAsync(createSPQuery);
        }

        private async Task CreateAddVehicleSP()
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'AddVehicle')
            BEGIN
                DROP PROCEDURE AddVehicle
            END";
            
            await _context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
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
            
            await _context.Database.ExecuteSqlRawAsync(createSPQuery);
        }

        private async Task CreateUpdateVehicleSP()
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'UpdateVehicle')
            BEGIN
                DROP PROCEDURE UpdateVehicle
            END";
            
            await _context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
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
            
            await _context.Database.ExecuteSqlRawAsync(createSPQuery);
        }

        private async Task CreateDeleteVehicleSP()
        {
            // Önce SP varsa sil
            var dropSPQuery = @"
            IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'DeleteVehicle')
            BEGIN
                DROP PROCEDURE DeleteVehicle
            END";
            
            await _context.Database.ExecuteSqlRawAsync(dropSPQuery);
            
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
            
            await _context.Database.ExecuteSqlRawAsync(createSPQuery);
        }
    }
} 