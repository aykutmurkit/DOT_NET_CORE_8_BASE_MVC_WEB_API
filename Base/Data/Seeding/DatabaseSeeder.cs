using Base.Data.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Base.Data.Seeding
{
    /// <summary>
    /// Veritabanı seed işlemlerini yönetir
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Tüm seed işlemlerini gerçekleştirir
        /// </summary>
        public async Task SeedAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // ISeeder interface'ini implement eden tüm sınıfları bul
            var seeders = GetSeeders();

            foreach (var seeder in seeders.OrderBy(s => s.Order))
            {
                try
                {
                    _logger.LogInformation("Seeding: {SeederName} başlatılıyor...", seeder.GetType().Name);
                    await seeder.SeedAsync(context);
                    _logger.LogInformation("Seeding: {SeederName} başarıyla tamamlandı.", seeder.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Seeding: {SeederName} sırasında hata oluştu: {ErrorMessage}", 
                        seeder.GetType().Name, ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Uygulamadaki tüm ISeeder implementasyonlarını bulur ve örneklerini oluşturur
        /// </summary>
        private List<ISeeder> GetSeeders()
        {
            var seeders = new List<ISeeder>();
            
            // ISeeder interface'ini implement eden tüm tipleri bul
            var seederTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && typeof(ISeeder).IsAssignableFrom(t));

            // Her tip için bir örnek oluştur
            foreach (var seederType in seederTypes)
            {
                var seeder = Activator.CreateInstance(seederType) as ISeeder;
                if (seeder != null)
                {
                    seeders.Add(seeder);
                }
            }

            return seeders;
        }
    }
} 