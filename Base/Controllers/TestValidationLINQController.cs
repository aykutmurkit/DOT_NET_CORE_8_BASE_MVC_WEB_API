using Base.Models.DTOs.Test;
using Base.Utilities;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Base.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestValidationLINQController : ControllerBase
    {
        [HttpPost("create/test")]
        public ActionResult<ApiResponse<object>> TestCreateValidation([FromBody] TestCreateDto dto)
        {
            // ValidationFilter sınıfı validasyon hatalarını global olarak yakalıyor
            // Bu nedenle burada özel bir validasyon kontrolü yapmaya gerek yok

            // Validasyon geçtiyse başarılı yanıt dön
            return Ok(ApiResponse<object>.Success(
                new { 
                    IsValidationPassed = true,
                    SentData = dto
                },
                "Test create validasyonu başarılı."
            ));
        }

        [HttpPost("update/test")]
        public ActionResult<ApiResponse<object>> TestUpdateValidation([FromBody] TestUpdateDto dto)
        {
            // ValidationFilter sınıfı validasyon hatalarını global olarak yakalıyor
            // Bu nedenle burada özel bir validasyon kontrolü yapmaya gerek yok

            // Varsayalım ki DB'de sadece 1-5 arası ID'ler var
            if (dto.Id > 5)
            {
                var errors = new Dictionary<string, List<string>>
                {
                    { "Id", new List<string> { "Belirtilen ID ile test kaydı bulunamadı." } }
                };
                
                return BadRequest(ApiResponse<object>.Error(
                    errors,
                    "Validasyon hatası oluştu."
                ));
            }

            // Validasyon geçtiyse başarılı yanıt dön
            return Ok(ApiResponse<object>.Success(
                new { 
                    IsValidationPassed = true,
                    SentData = dto
                },
                "Test update validasyonu başarılı."
            ));
        }

        [HttpGet("samples")]
        public ActionResult<ApiResponse<object>> GetTestSamples()
        {
            // Geçerli örnekler
            var validCreateSample = new TestCreateDto
            {
                Name = "Test Örneği",
                Description = "Bu bir test örneğidir. En az 10 karakter.",
                Value = 99.99m
            };

            var validUpdateSample = new TestUpdateDto
            {
                Id = 1,
                Name = "Test Örneği Güncel",
                Description = "Bu güncellenmiş bir test örneğidir. En az 10 karakter.",
                Value = 149.99m
            };

            // Geçersiz örnekler
            var invalidCreateSample = new
            {
                Name = "A", // 3 karakterden az (StringLength hatası)
                Description = "Kısa", // 10 karakterden az (MinLength hatası)
                Value = 0m // 0.01'den az (Range hatası)
            };

            var invalidUpdateSample = new
            {
                Id = 99, // DB'de olmayan ID
                Name = "A@#$%", // Regex hatası
                Description = new string('A', 501), // 500 karakterden fazla (StringLength hatası)
                Value = 1000000m // 999999.99'dan fazla (Range hatası)
            };

            // Tüm validasyon hatalarını gösteren örnek
            var allErrorsSample = new
            {
                // Name eksik (Required hatası)
                Description = "X", // 10 karakterden az (MinLength hatası)
                Value = -5m // 0.01'den az (Range hatası)
            };

            return Ok(ApiResponse<object>.Success(
                new
                {
                    ValidSamples = new 
                    {
                        Create = validCreateSample,
                        Update = validUpdateSample
                    },
                    InvalidSamples = new
                    {
                        Create = invalidCreateSample,
                        Update = invalidUpdateSample,
                        AllErrors = allErrorsSample
                    },
                    TestEndpoints = new[]
                    {
                        "/api/TestValidationLINQ/create/test",
                        "/api/TestValidationLINQ/update/test"
                    }
                },
                "Test örnekleri oluşturuldu."
            ));
        }
    }
} 