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
    public class DeviceLogLINQController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeviceLogLINQController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DeviceLog>>>> GetAll()
        {
            var logs = await _context.DeviceLogs
                .Include(l => l.Device)
                .ToListAsync();
                
            return ApiResponse<List<DeviceLog>>.Success(logs, "Cihaz logları başarıyla listelendi.");
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DeviceLog>>> GetById(int id)
        {
            var log = await _context.DeviceLogs
                .Include(l => l.Device)
                .FirstOrDefaultAsync(l => l.Id == id);
                
            if (log == null)
            {
                return this.NotFoundResponse<DeviceLog>("Log kaydı bulunamadı.");
            }
            
            return ApiResponse<DeviceLog>.Success(log, "Log kaydı başarıyla getirildi.");
        }

        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult<ApiResponse<List<DeviceLog>>>> GetByDeviceId(int deviceId)
        {
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
            {
                return this.NotFoundResponse<List<DeviceLog>>("Cihaz bulunamadı.");
            }

            var logs = await _context.DeviceLogs
                .Include(l => l.Device)
                .Where(l => l.DeviceId == deviceId)
                .ToListAsync();
                
            return ApiResponse<List<DeviceLog>>.Success(logs, $"{device.Name} cihazına ait loglar başarıyla listelendi.");
        }

        [HttpPost]
        [Authorize(Policy = "RequireDeveloperRole")]
        public async Task<ActionResult<ApiResponse<DeviceLog>>> Create(DeviceLog log)
        {
            var device = await _context.Devices.FindAsync(log.DeviceId);
            if (device == null)
            {
                return this.BadRequestResponse<DeviceLog>("Belirtilen cihaz bulunamadı.");
            }

            log.CreatedDate = DateTime.Now;
            
            _context.DeviceLogs.Add(log);
            await _context.SaveChangesAsync();
            
            // İlişkili Device entity'sini ekleyerek dönüyoruz
            await _context.Entry(log).Reference(l => l.Device).LoadAsync();
            
            return this.CreatedResponse(log, "Log kaydı başarıyla oluşturuldu.");
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireDeveloperRole")]
        public async Task<ActionResult<ApiResponse<DeviceLog>>> Update(int id, DeviceLog log)
        {
            if (id != log.Id)
            {
                return this.BadRequestResponse<DeviceLog>("ID eşleşmiyor.");
            }

            var existingLog = await _context.DeviceLogs.FindAsync(id);
            if (existingLog == null)
            {
                return this.NotFoundResponse<DeviceLog>("Güncellenecek log kaydı bulunamadı.");
            }

            // DeviceId değiştirilmek isteniyorsa, yeni Device'ın varlığını kontrol et
            if (existingLog.DeviceId != log.DeviceId)
            {
                var device = await _context.Devices.FindAsync(log.DeviceId);
                if (device == null)
                {
                    return this.BadRequestResponse<DeviceLog>("Belirtilen yeni cihaz bulunamadı.");
                }
            }

            // Sadece güncellenebilir alanları güncelle
            existingLog.Message = log.Message;
            existingLog.LogType = log.LogType;
            existingLog.Severity = log.Severity;
            existingLog.IsResolved = log.IsResolved;
            existingLog.ResolutionNotes = log.ResolutionNotes;
            
            // Eğer çözüldü olarak işaretlendiyse çözüm tarihini güncelle
            if (log.IsResolved && !existingLog.ResolvedDate.HasValue)
            {
                existingLog.ResolvedDate = DateTime.Now;
            }
            else if (!log.IsResolved)
            {
                existingLog.ResolvedDate = null;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await LogExists(id))
                {
                    return this.NotFoundResponse<DeviceLog>("Güncellenecek log kaydı bulunamadı.");
                }
                else
                {
                    return this.ServerErrorResponse<DeviceLog>("Log güncellenirken bir hata oluştu.");
                }
            }

            // İlişkili Device entity'sini ekleyerek dönüyoruz
            await _context.Entry(existingLog).Reference(l => l.Device).LoadAsync();
            
            return ApiResponse<DeviceLog>.Success(existingLog, "Log kaydı başarıyla güncellendi.");
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            var log = await _context.DeviceLogs.FindAsync(id);
            if (log == null)
            {
                return this.NotFoundResponse<object>("Silinecek log kaydı bulunamadı.");
            }

            _context.DeviceLogs.Remove(log);
            await _context.SaveChangesAsync();
            
            return this.NoContentResponse("Log kaydı başarıyla silindi.");
        }

        private async Task<bool> LogExists(int id)
        {
            return await _context.DeviceLogs.AnyAsync(e => e.Id == id);
        }
    }
} 