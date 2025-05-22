using Base.Data.Context;

namespace Base.Data.Seeding
{
    /// <summary>
    /// Seed data işlemleri için arayüz
    /// </summary>
    public interface ISeeder
    {
        /// <summary>
        /// Seed etme sirasini belirler. Dusuk sayilar once calisir.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Seed islemini gerceklestirir
        /// </summary>
        /// <param name="context">Veritabani baglanti context'i</param>
        Task SeedAsync(AppDbContext context);
    }
} 