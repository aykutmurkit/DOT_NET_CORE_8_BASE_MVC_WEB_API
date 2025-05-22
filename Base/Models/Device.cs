using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Base.Models
{
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
} 