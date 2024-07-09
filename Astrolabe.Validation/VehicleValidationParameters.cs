using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.RegularExpressions;
using CsvHelper;

namespace Astrolabe.Validation;

public enum VehicleFieldType
{
    Vehicle,
    Component,
    AxleGroup,
    Axle
}

public class VehicleValidationParameters
{
    public string? Field { get; set; }

    public string? Compare { get; set; }

    public string? IsDrive { get; set; }
}

public static partial class ValidationLoader
{
    public static Rule<VehicleDefinitionEdit> LoadRules()
    {
        using var reader = new StreamReader("SomePbs.csv");
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
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
                "Length" => new FilteredRule(VehicleFieldType.Vehicle,
                    rf => rf.VehicleRule(x => x.Length, ApplyComparison)),
                "TyreSize" => new FilteredRule(VehicleFieldType.Axle,
                    rf => rf.AxleRule(x => x.TyreSize, ApplyComparison)),
            };

            Rule<VehicleDefinitionEdit> ApplyComparison<TN>(RuleBuilder<VehicleDefinitionEdit, TN> builder, RuleFilters rf)
                where TN : struct
            {
                builder = (p.IsDrive, rf.IsDrive) switch
                {
                    ({ } driveVal, { } isDriveEx) when 
                        driveVal.ToLower().Trim() is { Length: > 0 } ld && ld[0] is var c => 
                        builder.When(c == 't' == isDriveEx),
                    _ => builder
                };
                var compare = p.Compare;
                if (compare is not null)
                {
                    var match = MyRegex().Match(compare);
                    var d = double.Parse(match.Groups[2].Value);
                    var compareFunc = match.Groups[1].Value switch
                    {
                        ">" => InbuiltFunction.Gt,
                        ">=" => InbuiltFunction.GtEq,
                        "=" or "" => InbuiltFunction.Eq,
                        "<>" => InbuiltFunction.Ne,
                        "<" => InbuiltFunction.Lt,
                        "<=" => InbuiltFunction.LtEq
                    };
                    return builder.CallInbuilt(compareFunc, d.ToExpr());
                }
                return builder.Must(x => new BoolExpr(true.ToExpr()));
            }
        }
    }

    [GeneratedRegex(@"([<>=]*)([\.\-\d]+)")]
    private static partial Regex MyRegex();

    internal record RuleFilters(
        PropertyValidator<VehicleDefinitionEdit, VehicleDefinitionEdit> VehicleProps,
        List<FilteredRule> RuleList,
        BoolExpr? IsDrive = null,
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
            return this with { AxleProps = a };
        }

        public Rule<VehicleDefinitionEdit> AxleGroupRule<TN>(Expression<Func<AxleGroupEdit, TN?>> getProp,
            Func<RuleBuilder<VehicleDefinitionEdit, TN>, Rule<VehicleDefinitionEdit>> create) where TN : struct
        {
            return create(AxleGroupProps!.RuleFor(getProp));
        }

        public Rule<VehicleDefinitionEdit> ComponentRule<TN>(Expression<Func<VehicleComponentEdit, TN?>> getProp,
            Func<RuleBuilder<VehicleDefinitionEdit, TN>, Rule<VehicleDefinitionEdit>> create) where TN : struct
        {
            return create(ComponentProps!.RuleFor(getProp));
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