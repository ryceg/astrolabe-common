using FluentValidation;

namespace Astrolabe.ColumnEditor;

public static class ColumnValidationExtensions
{
    public static PropertyColumnBuilder<TEdit, TDb, T, T2> WithValidation<TEdit, TDb, T, T2>(
        this PropertyColumnBuilder<TEdit, TDb, T, T2> builder, Action<IRuleBuilderInitial<TEdit, T>> validation)
    {
        builder.Attributes["Validator"] = (AbstractValidator<TEdit> v) => validation(v.RuleFor(builder.EditValueExpression));
        return builder;
    }

    public static void ApplyValidation<TEdit, TDb>(this AbstractValidator<TEdit> validator,
        ColumnEditor<TEdit, TDb> column)
    {
        if (column.Attributes.TryGetValue("Validator", out var columnValidator))
        {
            ((Action<AbstractValidator<TEdit>>)columnValidator)(validator);
        }
    }
}