using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace Astrolabe.Validation;

public enum VehicleFieldType
{
    Vehicle,
    Component,
    AxleGroup,
    Axle
}

public enum TypeForComparison
{
    Number,
    Bool
}

public class VehicleValidationParameters
{
    public ConstraintType Field { get; set; }

    public string? Compare { get; set; }

    public string? IsDrive { get; set; }
    
    public string? Tyres { get; set; }
}

public static partial class ValidationLoader
{
    public static Rule<VehicleDefinitionEdit> LoadRules()
    {
        using var reader = new StreamReader("SomePbs.csv");
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };
        using var csv = new CsvReader(reader, config);
        var fieldRules = csv.GetRecords<VehicleValidationParameters>().Select(MakeDefinition).ToList();
        var filterValues = new RuleFilters(new PropertyValidator<VehicleDefinitionEdit, VehicleDefinitionEdit>(null), fieldRules);

        return CreateAllRules(filterValues);
        
        MultiRule<VehicleDefinitionEdit> CreateAllRules(RuleFilters ruleFilters)
        {
            var vehicleRules = ruleFilters.CreateRules(VehicleFieldType.Vehicle);
            var otherRules = ruleFilters.VehicleProps.RuleForEach(x => x.Components,
                c =>
                {
                    ruleFilters = ruleFilters.WithComponents(c);
                    return ruleFilters.CreateRules(VehicleFieldType.Component).AddRule(
                        c.RuleForEach(x => x.AxleGroups, ag =>
                        {
                            ruleFilters = ruleFilters.WithAxleGroups(ag);
                            return ruleFilters.CreateRules(VehicleFieldType.AxleGroup).AddRule(
                                ag.RuleForEach(x => x.Axles, a =>
                                {
                                    return ruleFilters.WithAxles(a).CreateRules(VehicleFieldType.Axle);
                                }));
                        }));
                });
            return vehicleRules.AddRule(otherRules);
        }
        
        FilteredRule MakeDefinition(VehicleValidationParameters p)
        {
            return p.Field switch
            {
                ConstraintType.Length => Vehicle(x => x.Length),
                ConstraintType.Width => Vehicle(x => x.Width),
                ConstraintType.Height => Vehicle(x => x.Height),
                ConstraintType.TyreSize => Axle(x => x.TyreSize),
                ConstraintType.GCW => Axle(x => x.ContactWidth),
                ConstraintType.TareMass => Component(x => x.Tare),
                ConstraintType.GroupOperatingMass => Group(x => x.OperatingMass),
                ConstraintType.AxleOperatingMass => Axle(x => x.OperatingMass),
                ConstraintType.AxleSpacing => Axle(x => x.Spacing),
                ConstraintType.Startability => Vehicle(x => x.Startability),
                ConstraintType.GradeabilityA => Vehicle(x => x.GradeabilityA),
                ConstraintType.TrackingAbilityOnStraightPath => Vehicle(x => x.TrackingAbilityOnStraightPath),
                ConstraintType.LowSpeedSweptPath => Vehicle(x => x.LowSpeedSweptPath),
                ConstraintType.HighSpeedTransientOfftracking => Vehicle(x => x.HighSpeedTransientOfftracking),
                _ => throw new ArgumentOutOfRangeException($"{p.Field}")
            };

            FilteredRule Vehicle<TN>(Expression<Func<VehicleDefinitionEdit, TN?>> e) where TN : struct, ISignedNumber<TN>
            {
                return new FilteredRule(VehicleFieldType.Vehicle, rf => rf.VehicleRule(e, ApplyNumberComparison));
            }
            
            FilteredRule Axle<TN>(Expression<Func<AxleEdit, TN?>> e) where TN : struct, ISignedNumber<TN>
            {
                return new FilteredRule(VehicleFieldType.Axle, rf => rf.AxleRule(e, ApplyNumberComparison));
            }

            FilteredRule Component<TN>(Expression<Func<VehicleComponentEdit, TN?>> e) where TN : struct, ISignedNumber<TN>
            {
                return new FilteredRule(VehicleFieldType.Component, rf => rf.ComponentRule(e, ApplyNumberComparison));
            }

            
            FilteredRule Group<TN>(Expression<Func<AxleGroupEdit, TN?>> e) where TN : struct, ISignedNumber<TN>
            {
                return new FilteredRule(VehicleFieldType.AxleGroup, rf => rf.AxleGroupRule(e, ApplyNumberComparison));
            }

            Rule<VehicleDefinitionEdit> ApplyNumberComparison<TN>(RuleBuilder<VehicleDefinitionEdit, TN> builder, RuleFilters rf)
                where TN : struct, ISignedNumber<TN>
            {
                return ApplyComparison(builder, rf, TypeForComparison.Number);
            }

            Rule<VehicleDefinitionEdit> ApplyComparison<TN>(RuleBuilder<VehicleDefinitionEdit, TN> builder, RuleFilters rf, TypeForComparison valueType)
                where TN : struct
            {
                builder = ParseAndApply(builder, TypeForComparison.Bool, rf.IsDrive, p.IsDrive);
                builder = ParseAndApply(builder, valueType, builder.GetExpr(), p.Compare, true);
                return builder as Rule<VehicleDefinitionEdit> ?? builder.MustExpr(new BoolValue(true));
            }
        }

    }

    public static RuleBuilder<VehicleDefinitionEdit, TN> ParseAndApply<TN>(RuleBuilder<VehicleDefinitionEdit, TN> builder, TypeForComparison fieldType, 
        Expr? valueToCompare, string? comparison, bool must = false)
    {
        if (valueToCompare == null || string.IsNullOrWhiteSpace(comparison))
            return builder;
        var match = MyRegex().Match(comparison);
        var compareFunc = match.Groups[1].Value switch
        {
            ">" => InbuiltFunction.Gt,
            ">=" => InbuiltFunction.GtEq,
            "=" or "" => InbuiltFunction.Eq,
            "<>" => InbuiltFunction.Ne,
            "<" => InbuiltFunction.Lt,
            "<=" => InbuiltFunction.LtEq
        };
        var requiredExpr = fieldType switch
        {
            TypeForComparison.Bool => (char.ToLower(match.Groups[0].Value[0]) is 't' or 'y').ToExpr(),
            TypeForComparison.Number => double.Parse(match.Groups[2].Value).ToExpr(),
        };
        var comparisonExpr = new CallExpr(compareFunc, [valueToCompare, requiredExpr]);
        return must ? builder.MustExpr(comparisonExpr) : builder.WhenExpr(comparisonExpr);
    }

    [GeneratedRegex(@"([<>=]*)(.+)")]
    private static partial Regex MyRegex();

    internal record RuleFilters(
        PropertyValidator<VehicleDefinitionEdit, VehicleDefinitionEdit> VehicleProps,
        List<FilteredRule> RuleList,
        BoolExpr? IsDrive = null,
        NumberExpr? Tyres = null,
        PropertyValidator<VehicleDefinitionEdit, VehicleComponentEdit>? ComponentProps = null,
        PropertyValidator<VehicleDefinitionEdit, AxleGroupEdit>? AxleGroupProps = null,
        PropertyValidator<VehicleDefinitionEdit, AxleEdit>? AxleProps = null)
    {
        public RuleFilters WithComponents(PropertyValidator<VehicleDefinitionEdit, VehicleComponentEdit> c)
        {
            return this with { ComponentProps = c };
        }

        public RuleFilters WithAxleGroups(PropertyValidator<VehicleDefinitionEdit, AxleGroupEdit> ag)
        {
            return this with
            {
                AxleGroupProps = ag, 
                IsDrive = ComponentProps!.Index == 0 & ag.Index == 1
            };
        }

        public RuleFilters WithAxles(PropertyValidator<VehicleDefinitionEdit, AxleEdit> a)
        {
            return this with { AxleProps = a, Tyres = a.Get(x => x.Tyres)};
        }

        public Rule<VehicleDefinitionEdit> AxleGroupRule<TN>(Expression<Func<AxleGroupEdit, TN?>> getProp,
            Func<RuleBuilder<VehicleDefinitionEdit, TN>, RuleFilters, Rule<VehicleDefinitionEdit>> create) where TN : struct
        {
            return create(AxleGroupProps!.RuleFor(getProp), this);
        }

        public Rule<VehicleDefinitionEdit> ComponentRule<TN>(Expression<Func<VehicleComponentEdit, TN?>> getProp,
            Func<RuleBuilder<VehicleDefinitionEdit, TN>, RuleFilters, Rule<VehicleDefinitionEdit>> create) where TN : struct
        {
            return create(ComponentProps!.RuleFor(getProp), this);
        }

        public Rule<VehicleDefinitionEdit> VehicleRule<TN>(Expression<Func<VehicleDefinitionEdit, TN?>> getProp,
            Func<RuleBuilder<VehicleDefinitionEdit, TN>, RuleFilters, Rule<VehicleDefinitionEdit>> create) where TN : struct
        {
            return create(VehicleProps.RuleFor(getProp), this);
        }

        public Rule<VehicleDefinitionEdit> AxleRule<TN>(Expression<Func<AxleEdit, TN?>> getProp,
            Func<RuleBuilder<VehicleDefinitionEdit, TN>, RuleFilters, Rule<VehicleDefinitionEdit>> create) where TN : struct
        {
            return create(AxleProps!.RuleFor(getProp), this);
        }

        public MultiRule<VehicleDefinitionEdit> CreateRules(VehicleFieldType fieldType)
        {
            return new MultiRule<VehicleDefinitionEdit>(
                RuleList.Where(x => x.FieldType == fieldType).Select(x => x.CreateRule(this))
            );
        }
    }

    internal record FilteredRule(VehicleFieldType FieldType, Func<RuleFilters, Rule<VehicleDefinitionEdit>> CreateRule);

}