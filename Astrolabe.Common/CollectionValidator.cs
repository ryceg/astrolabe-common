using System.Collections.Generic;
using FluentValidation;

namespace Astrolabe.Common;

public class CollectionValidator<TA> : AbstractValidator<IEnumerable<TA>>
{
    public CollectionValidator(IValidator<TA> validator)
    {
        RuleForEach(a => a).SetValidator(validator);
    }
}