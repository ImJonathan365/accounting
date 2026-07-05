using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class CreateJournalEntryDtoValidator : AbstractValidator<CreateJournalEntryDto>
{
    public CreateJournalEntryDtoValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha es requerida.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida.")
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres.");

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("La referencia no puede superar 100 caracteres.")
            .When(x => x.Reference is not null);

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("El asiento debe tener al menos 2 líneas.")
            .Must(l => l.Count >= 2).WithMessage("El asiento debe tener al menos 2 líneas.");

        RuleFor(x => x.Lines)
            .Must(l => l.Sum(line => line.Debit) == l.Sum(line => line.Credit))
            .WithMessage("La suma de débitos debe ser igual a la suma de créditos.")
            .When(x => !x.IsDraft && x.Lines.Count >= 2);

        RuleForEach(x => x.Lines).SetValidator(new CreateJournalLineDtoValidator()).When(x => !x.IsDraft);
        RuleForEach(x => x.Lines).SetValidator(new DraftJournalLineDtoValidator()).When(x => x.IsDraft);
    }
}

public class UpdateJournalEntryDtoValidator : AbstractValidator<UpdateJournalEntryDto>
{
    public UpdateJournalEntryDtoValidator()
    {
        RuleFor(x => x.Date).NotEmpty().WithMessage("La fecha es requerida.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción es requerida.").MaximumLength(500);
        RuleFor(x => x.Reference).MaximumLength(100).When(x => x.Reference is not null);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count >= 2).WithMessage("El asiento debe tener al menos 2 líneas.");
        RuleForEach(x => x.Lines).SetValidator(new DraftJournalLineDtoValidator());
    }
}

public class DraftJournalLineDtoValidator : AbstractValidator<CreateJournalLineDto>
{
    public DraftJournalLineDtoValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("La cuenta es requerida.");
        RuleFor(x => x.Debit).GreaterThanOrEqualTo(0).WithMessage("El débito no puede ser negativo.");
        RuleFor(x => x.Credit).GreaterThanOrEqualTo(0).WithMessage("El crédito no puede ser negativo.");
        RuleFor(x => x).Must(l => !(l.Debit > 0 && l.Credit > 0))
            .WithMessage("Una línea no puede tener débito y crédito simultáneamente.")
            .OverridePropertyName("lines");
        RuleFor(x => x.Note).MaximumLength(250).WithMessage("La nota no puede superar 250 caracteres.").When(x => x.Note is not null);
    }
}

public class CreateJournalLineDtoValidator : AbstractValidator<CreateJournalLineDto>
{
    public CreateJournalLineDtoValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("La cuenta es requerida.");

        RuleFor(x => x.Debit)
            .GreaterThanOrEqualTo(0).WithMessage("El débito no puede ser negativo.");

        RuleFor(x => x.Credit)
            .GreaterThanOrEqualTo(0).WithMessage("El crédito no puede ser negativo.");

        RuleFor(x => x)
            .Must(l => (l.Debit > 0) != (l.Credit > 0))
            .WithMessage("Cada línea debe tener débito O crédito, no ambos ni ninguno.")
            .OverridePropertyName("lines");

        RuleFor(x => x.Note)
            .MaximumLength(250).WithMessage("La nota no puede superar 250 caracteres.")
            .When(x => x.Note is not null);
    }
}
