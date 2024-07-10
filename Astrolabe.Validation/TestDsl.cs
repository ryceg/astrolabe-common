using System.ComponentModel;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using Astrolabe.Common;

namespace Astrolabe.Validation;

public enum BonnetType
{
    Short,
    Long
}

public enum AxleStyle
{
    Closed,
    Spread
}

public enum VehicleLoadType
{
    Unladen,
    Laden,
    GeneralFreight,
    Livestock,
    Refrigerated,
    Bulk,
    Vehicles,
    Logs,
    GrainOilseedsPulses,
    Liquids,
    BaledHay,
    Fertiliser
}

public record AxleEdit(
    double? OperatingMass,
    double? Spacing,
    int? Tyres,
    double? TyreSize,
    double? ContactWidth,
    bool? Steerable
);

public record AxleGroupEdit(
    bool LoadSharing,
    bool? RoadFriendlySuspension,
    double? OperatingMass,
    ICollection<AxleEdit> Axles
);

public enum VehicleComponentType
{
    PrimeMover,
    Dolly,
    PlatformTrailer,
    BlockTruck,
    LeadTrailer,
    SemiTrailer,
    LowLoader,
    ConverterDolly,
    DogTrailer,
    PigTrailer,
    TagTrailer,
    RigidTruck
}

public record VehicleComponentEdit(
    VehicleComponentType Type,
    double? Tare,
    AxleStyle? AxleStyle,
    BonnetType? BonnetType,
    ICollection<AxleGroupEdit> AxleGroups
);

public record VehicleDefinitionEdit(
    ICollection<VehicleComponentEdit> Components,
    bool? SteerAxleCompliant,
    double? Length,
    double? Width,
    double? Height,
    IEnumerable<VehicleLoadType> Load,
    int? PerformanceLevel,
    double? Startability,
    double? GradeabilityA,
    double? TrackingAbilityOnStraightPath,
    double? LowSpeedSweptPath,
    double? HighSpeedTransientOfftracking,
    double NotNullable
);

public enum ConstraintType
{
    GCW,
    TyreSize,
    TareMass,
    GroupOperatingMass,
    AxleOperatingMass,
    AxleSpacing,
    Startability,
    GradeabilityA,
    TrackingAbilityOnStraightPath,
    LowSpeedSweptPath,
    HighSpeedTransientOfftracking,
    Length,
    Width,
    Height
}
