using AstrolabeApp.Data.EF;
using AstrolabeApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AstrolabeApp.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if database already has data
        if (await context.Teas.AnyAsync())
        {
            Console.WriteLine("Database already contains data. Skipping seed.");
            return;
        }

        Console.WriteLine("Seeding database with sample data...");

        var teas = new List<Tea>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Peppermint,
                NumberOfSugars = 0,
                MilkAmount = MilkAmount.None,
                IncludeSpoon = false,
                BrewNotes = "Refreshing peppermint, no sugar",
                FlavorNotes = "Cool and refreshing with a clean finish",
                BrewTimeSeconds = 180, // 3 minutes
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Peppermint,
                NumberOfSugars = 1,
                MilkAmount = MilkAmount.None,
                IncludeSpoon = true,
                BrewNotes = "Peppermint with a touch of sweetness",
                FlavorNotes = "Cool and refreshing with a hint of sweetness",
                BrewTimeSeconds = 180,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Rooibos,
                NumberOfSugars = 0,
                MilkAmount = MilkAmount.None,
                IncludeSpoon = true,
                BrewNotes = "Rooibos with spoon for stirring",
                FlavorNotes = "Naturally sweet with earthy notes",
                BrewTimeSeconds = 300, // 5 minutes
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Purple,
                NumberOfSugars = 0,
                MilkAmount = MilkAmount.Splash,
                IncludeSpoon = false,
                BrewNotes = "Purple tea with a splash of milk",
                FlavorNotes = "Unique and antioxidant-rich with berry hints",
                BrewTimeSeconds = 240, // 4 minutes
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Purple,
                NumberOfSugars = 0,
                MilkAmount = MilkAmount.Normal,
                IncludeSpoon = false,
                BrewNotes = "Purple tea with milk",
                FlavorNotes = "Creamy with subtle berry undertones",
                BrewTimeSeconds = 240,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Purple,
                NumberOfSugars = 2,
                MilkAmount = MilkAmount.Normal,
                IncludeSpoon = true,
                BrewNotes = "Purple tea with milk and sugar",
                FlavorNotes = "Sweet and creamy with berry notes",
                BrewTimeSeconds = 240,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Black,
                NumberOfSugars = 1,
                MilkAmount = MilkAmount.Normal,
                IncludeSpoon = true,
                BrewNotes = "Classic English breakfast style",
                FlavorNotes = "Bold and robust with malty undertones",
                BrewTimeSeconds = 240, // 4 minutes
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Green,
                NumberOfSugars = 0,
                MilkAmount = MilkAmount.None,
                IncludeSpoon = false,
                BrewNotes = "Light and delicate green tea",
                FlavorNotes = "Fresh and grassy with a delicate sweetness",
                BrewTimeSeconds = 180, // 3 minutes - lower temp, shorter time
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Oolong,
                NumberOfSugars = 0,
                MilkAmount = MilkAmount.None,
                IncludeSpoon = false,
                BrewNotes = "Traditional oolong preparation",
                FlavorNotes = "Complex and floral with a smooth finish",
                BrewTimeSeconds = 300, // 5 minutes
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.White,
                NumberOfSugars = 0,
                MilkAmount = MilkAmount.None,
                IncludeSpoon = false,
                BrewNotes = "Delicate white tea",
                FlavorNotes = "Light and subtle with hints of honey",
                BrewTimeSeconds = 240, // 4 minutes
            },
            new()
            {
                Id = Guid.NewGuid(),
                Type = TeaType.Herbal,
                NumberOfSugars = 1,
                MilkAmount = MilkAmount.None,
                IncludeSpoon = true,
                BrewNotes = "Caffeine-free herbal blend",
                FlavorNotes = "Aromatic and soothing, caffeine-free",
                BrewTimeSeconds = 360, // 6 minutes - herbal needs longer
            },
        };

        context.Teas.AddRange(teas);
        await context.SaveChangesAsync();

        Console.WriteLine($"Seeded {teas.Count} tea records.");
    }
}
