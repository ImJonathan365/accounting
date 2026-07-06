using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class CreateInvoiceDtoValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceDtoValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ContactId).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Date).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$");
        RuleFor(x => x.DueDate).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$");
        RuleFor(x => x.ArApAccountId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("La factura debe tener al menos una línea.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            l.RuleFor(x => x.Quantity).GreaterThan(0);
            l.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.AccountId).NotEmpty();
        });
    }
}

public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentDtoValidator()
    {
        RuleFor(x => x.Date).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentAccountId).NotEmpty();
    }
}
