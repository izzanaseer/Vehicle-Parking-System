using Microsoft.EntityFrameworkCore;
using ParkingSystem.Models;
namespace ParkingSystem.Data;

public class ParkingDbContext(DbContextOptions<ParkingDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers { get; set; }                            // Properties that represent mapping between our objects and DB tables
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ParkingSlot> ParkingSlots { get; set; }
    public DbSet<ParkingTicket> ParkingTickets { get; set; }
    public DbSet<VehicleRate> VehicleRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(v => v.VehicleNumber).IsUnique();

        modelBuilder.Entity<ParkingSlot>().HasData(
            new ParkingSlot { Id = 1, SlotNumber = "C1", SlotType = VehicleType.Car, IsOccupied = false },
            new ParkingSlot { Id = 2, SlotNumber = "C2", SlotType = VehicleType.Car, IsOccupied = false },
            new ParkingSlot { Id = 3, SlotNumber = "C3", SlotType = VehicleType.Car, IsOccupied = false },
            new ParkingSlot { Id = 4, SlotNumber = "B1", SlotType = VehicleType.Bike, IsOccupied = false },
            new ParkingSlot { Id = 5, SlotNumber = "B2", SlotType = VehicleType.Bike, IsOccupied = false },
            new ParkingSlot { Id = 6, SlotNumber = "T1", SlotType = VehicleType.Truck, IsOccupied = false }
        );

        modelBuilder.Entity<VehicleRate>().HasData(
            new VehicleRate { Id = 1, Type = VehicleType.Car, RatePerHour = 50 },
            new VehicleRate { Id = 2, Type = VehicleType.Bike, RatePerHour = 20 },
            new VehicleRate { Id = 3, Type = VehicleType.Truck, RatePerHour = 100 }
        );

        modelBuilder.Entity<ParkingTicket>()
            .Property(t => t.FeeAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<VehicleRate>()
            .Property(r => r.RatePerHour)
            .HasPrecision(18, 2);
    }
}