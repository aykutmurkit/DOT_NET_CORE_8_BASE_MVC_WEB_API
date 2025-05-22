using System;
using System.ComponentModel.DataAnnotations;

namespace Base.Models.DTOs.Test
{
    public class TestUpdateDto
    {
        [Required(ErrorMessage = "Test ID zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ID değeri girilmelidir.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Test adı zorunludur.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Test adı 3-100 karakter arasında olmalıdır.")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Test adı yalnızca harf, rakam, boşluk ve tire içerebilir.")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [MinLength(10, ErrorMessage = "Açıklama en az 10 karakter olmalıdır.")]
        [DataType(DataType.MultilineText, ErrorMessage = "Açıklama metin formatında olmalıdır.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Değer bilgisi zorunludur.")]
        [Range(0.01, 999999.99, ErrorMessage = "Değer 0.01 ile 999999.99 arasında olmalıdır.")]
        [DataType(DataType.Currency, ErrorMessage = "Değer sayısal formatta olmalıdır.")]
        public decimal Value { get; set; }
    }
} 