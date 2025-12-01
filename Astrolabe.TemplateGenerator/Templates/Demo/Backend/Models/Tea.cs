using Astrolabe.Annotation;
using Microsoft.EntityFrameworkCore;

namespace __ProjectName__.Models;

public class Tea
{
    public Guid Id { get; set; }
    public TeaType Type { get; set; }
    public int NumberOfSugars { get; set; }
    public MilkAmount MilkAmount { get; set; }
    public bool IncludeSpoon { get; set; }
    public string? BrewNotes { get; set; }
    public string? FlavorNotes { get; set; }
    public int BrewTimeSeconds { get; set; } = 180; // Default 3 minutes
}

[JsonString]
public enum TeaType
{
    Black,
    Green,
    Oolong,
    White,
    Herbal,
    Rooibos,
    Purple,
    Peppermint,
}

[JsonString]
public enum MilkAmount
{
    None,
    Splash,
    Normal,
    Extra,
}
