using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class CreateBudgetDtoValidator : AbstractValidator<CreateBudgetDto>
{
    public CreateBudgetDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Year).InclusiveBetween(2000, DateTime.UtcNow.Year + 5);
    }
}

public class UpsertBudgetLineDtoValidator : AbstractValidator<UpsertBudgetLineDto>
{
    public UpsertBudgetLineDtoValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}
