using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class CreateRecurringEntryDtoValidator : AbstractValidator<CreateRecurringEntryDto>
{
    public CreateRecurringEntryDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida.")
            .MaximumLength(300).WithMessage("Máximo 300 caracteres.");

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("La referencia no puede superar 100 caracteres.")
            .When(x => x.Reference is not null);

        RuleFor(x => x.Frequency)
            .IsInEnum().WithMessage("La frecuencia no es válida.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Debe incluir al menos una línea.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty().WithMessage("La cuenta es requerida.");
            line.RuleFor(l => l.Debit + l.Credit)
                .GreaterThan(0).WithMessage("Cada línea debe tener un monto mayor a 0.");
        });
    }
}

public class UpdateRecurringEntryDtoValidator : AbstractValidator<UpdateRecurringEntryDto>
{
    public UpdateRecurringEntryDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción no puede estar vacía.")
            .MaximumLength(300).WithMessage("Máximo 300 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("La referencia no puede superar 100 caracteres.")
            .When(x => x.Reference is not null);

        RuleFor(x => x.Frequency)
            .IsInEnum().WithMessage("La frecuencia no es válida.")
            .When(x => x.Frequency is not null);
    }
}
