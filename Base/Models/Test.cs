using System;
using System.ComponentModel.DataAnnotations;

namespace Base.Models
{
    public class Test
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Test adı zorunludur.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Test adı 3-100 karakter arasında olmalıdır.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Değer bilgisi zorunludur.")]
        [Range(0.01, 999999.99, ErrorMessage = "Değer 0.01 ile 999999.99 arasında olmalıdır.")]
        public decimal Value { get; set; }

        [Required(ErrorMessage = "Miktar bilgisi zorunludur.")]
        [Range(0, 10000, ErrorMessage = "Miktar 0 ile 10000 arasında olmalıdır.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Kategori zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Kategori 2-50 karakter arasında olmalıdır.")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Aktif durumu belirtilmelidir.")]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Oluşturma tarihi zorunludur.")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
} 