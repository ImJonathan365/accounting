using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class SetOpeningBalancesRequestValidator : AbstractValidator<SetOpeningBalancesRequest>
{
    public SetOpeningBalancesRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$")
            .WithMessage("La fecha debe tener el formato YYYY-MM-DD.");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Debe incluir al menos una línea.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.Debit).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.Credit).GreaterThanOrEqualTo(0);
        });
    }
}
