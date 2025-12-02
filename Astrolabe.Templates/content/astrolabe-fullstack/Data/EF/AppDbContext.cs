//#if (IncludeDemoData || IncludeLocalUsers)
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

//#if (IncludeLocalUsers)
    public DbSet<User> Users { get; set; }
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

//#if (IncludeLocalUsers)
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(255);
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.MfaNumber).HasMaxLength(20);
        });
//#endif
    }
}
