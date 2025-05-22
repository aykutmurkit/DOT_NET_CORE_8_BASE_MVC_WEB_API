using Base.Controllers;
using Base.Models;
using Microsoft.EntityFrameworkCore;

namespace Base.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceLog> DeviceLogs { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        
    }
} 