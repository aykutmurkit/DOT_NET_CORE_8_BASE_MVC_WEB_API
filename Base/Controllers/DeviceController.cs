using Base.Data.Context;
using Base.Models;
using Base.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Base.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeviceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<Device>>>> GetAll([FromQuery] bool includeLogs = false)
        {
            // Eğer logları da isteniyorsa Include ile getir
            IQueryable<Device> query = _context.Devices;
            
            if (includeLogs)
            {
                query = query.Include(d => d.Logs);
            }
            
            var devices = await query.ToListAsync();
            return ApiResponse<List<Device>>.Success(devices, "Cihazlar başarıyla listelendi.");
        }

        /// <summary>
        /// Stored Procedure kullanarak aktif cihazların id ve isimlerini getirir
        /// </summary>
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

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<Device>>> GetById(int id, [FromQuery] bool includeLogs = false)
        {
            // Eğer logları da isteniyorsa Include ile getir
            Device device;
            
            if (includeLogs)
            {
                device = await _context.Devices
                    .Include(d => d.Logs)
                    .FirstOrDefaultAsync(d => d.Id == id);
            }
            else
            {
                device = await _context.Devices.FindAsync(id);
            }
            
            if (device == null)
            {
                return this.NotFoundResponse<Device>("Cihaz bulunamadı.");
            }
            return ApiResponse<Device>.Success(device, "Cihaz başarıyla getirildi.");
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<ApiResponse<Device>>> Create(Device device)
        {
            // Entity validasyonları modelde olduğu için burada tekrar validasyon yapmamıza gerek yok
            device.CreatedDate = DateTime.Now;
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            
            return this.CreatedResponse(device, "Cihaz başarıyla oluşturuldu.");
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<ApiResponse<Device>>> Update(int id, Device device)
        {
            if (id != device.Id)
            {
                return this.BadRequestResponse<Device>("ID eşleşmiyor.");
            }

            var existingDevice = await _context.Devices.FindAsync(id);
            if (existingDevice == null)
            {
                return this.NotFoundResponse<Device>("Güncellenecek cihaz bulunamadı.");
            }

            existingDevice.Name = device.Name;
            existingDevice.Description = device.Description;
            existingDevice.IsActive = device.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await DeviceExists(id))
                {
                    return this.NotFoundResponse<Device>("Güncellenecek cihaz bulunamadı.");
                }
                else
                {
                    return this.ServerErrorResponse<Device>("Cihaz güncellenirken bir hata oluştu.");
                }
            }

            return ApiResponse<Device>.Success(existingDevice, "Cihaz başarıyla güncellendi.");
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireSuperAdminRole")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var device = await _context.Devices
                .Include(d => d.Logs)
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (device == null)
            {
                return this.NotFoundResponse<object>("Silinecek cihaz bulunamadı.");
            }

            // Cascade delete bunu otomatik olarak yapacak ancak
            // log sayısını kontrol etmek istiyorsak:
            int logCount = device.Logs.Count;

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            
            return this.NoContentResponse($"Cihaz ve bağlı {logCount} log kaydı başarıyla silindi.");
        }

        // Cihaza ait log ekleme metodu
        [HttpPost("{deviceId}/logs")]
        [Authorize(Policy = "RequireDeveloperRole")]
        public async Task<ActionResult<ApiResponse<DeviceLog>>> AddLog(int deviceId, DeviceLog log)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
            {
                return this.NotFoundResponse<DeviceLog>("Cihaz bulunamadı.");
            }

            // DeviceId parametresini URI'den alıyoruz
            log.DeviceId = deviceId;
            log.CreatedDate = DateTime.Now;
            
            _context.DeviceLogs.Add(log);
            await _context.SaveChangesAsync();
            
            log.Device = device; // İlişkiyi manuel olarak ayarlıyoruz
            
            return this.CreatedResponse(log, $"'{device.Name}' cihazına log kaydı başarıyla eklendi.");
        }

        // Cihaza ait tüm logları getirme metodu
        [HttpGet("{deviceId}/logs")]
        public async Task<ActionResult<ApiResponse<List<DeviceLog>>>> GetLogs(int deviceId)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
            {
                return this.NotFoundResponse<List<DeviceLog>>("Cihaz bulunamadı.");
            }

            var logs = await _context.DeviceLogs
                .Where(l => l.DeviceId == deviceId)
                .ToListAsync();
                
            return ApiResponse<List<DeviceLog>>.Success(logs, $"'{device.Name}' cihazına ait {logs.Count} log kaydı listelendi.");
        }

        private async Task<bool> DeviceExists(int id)
        {
            return await _context.Devices.AnyAsync(e => e.Id == id);
        }
    }

    /// <summary>
    /// Cihaz ID ve isimlerini taşıyan DTO sınıfı
    /// </summary>
    public class DeviceNameDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
} 