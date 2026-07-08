using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class CreateTaxRateDtoValidator : AbstractValidator<CreateTaxRateDto>
{
    public CreateTaxRateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Rate).InclusiveBetween(0, 100);
        RuleFor(x => x.TaxAccountId).NotEmpty();
    }
}

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.DefaultPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AccountId).NotEmpty();
    }
}
