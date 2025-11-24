//#if (IncludeDemoData)
using AstrolabeApp.Models;
//#endif
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AstrolabeApp.Data.EF;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

//#if (IncludeDemoData)
    public DbSet<Tea> Teas { get; set; }
//#endif

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

//#if (IncludeDemoData)
        // Configure Tea entity to use string conversion for enums
        modelBuilder.Entity<Tea>()
            .Property(t => t.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Tea>()
            .Property(t => t.MilkAmount)
            .HasConversion<string>();
//#endif
    }
}
