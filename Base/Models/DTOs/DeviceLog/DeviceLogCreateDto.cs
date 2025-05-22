using System;
using System.ComponentModel.DataAnnotations;

namespace Base.Models.DTOs.DeviceLog
{
    public class DeviceLogCreateDto
    {
        [Required(ErrorMessage = "Cihaz ID zorunludur.")]
        public int DeviceId { get; set; }

        [Required(ErrorMessage = "Log mesajı zorunludur.")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Log mesajı 2-500 karakter arasında olmalıdır.")]
        public string Message { get; set; }

        [Required(ErrorMessage = "Log türü zorunludur.")]
        [StringLength(50, ErrorMessage = "Log türü en fazla 50 karakter olabilir.")]
        public string LogType { get; set; }

        [Range(1, 5, ErrorMessage = "Önem seviyesi 1-5 arasında olmalıdır.")]
        public int Severity { get; set; } = 1;

        [Required(ErrorMessage = "Durumu zorunludur.")]
        public bool IsResolved { get; set; } = false;

        [StringLength(500, ErrorMessage = "Çözüm açıklaması en fazla 500 karakter olabilir.")]
        public string ResolutionNotes { get; set; }
    }
} 