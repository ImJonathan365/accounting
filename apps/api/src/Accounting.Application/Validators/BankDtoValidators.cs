using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class CreateBankAccountDtoValidator : AbstractValidator<CreateBankAccountDto>
{
    public CreateBankAccountDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankName).MaximumLength(200).When(x => x.BankName is not null);
        RuleFor(x => x.AccountNumber).MaximumLength(100).When(x => x.AccountNumber is not null);
        RuleFor(x => x.LinkedAccountId).NotEmpty();
    }
}

public class ImportBankTransactionDtoValidator : AbstractValidator<ImportBankTransactionDto>
{
    public ImportBankTransactionDtoValidator()
    {
        RuleFor(x => x.Date).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Fecha debe ser yyyy-MM-dd.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).NotEqual(0).WithMessage("El monto no puede ser cero.");
    }
}
