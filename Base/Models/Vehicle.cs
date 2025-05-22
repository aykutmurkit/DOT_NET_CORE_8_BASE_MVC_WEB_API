using System;
using System.ComponentModel.DataAnnotations;

namespace Base.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Araç adı zorunludur.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Araç adı 3-100 karakter arasında olmalıdır.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Oluşturma tarihi zorunludur.")]
        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
} 