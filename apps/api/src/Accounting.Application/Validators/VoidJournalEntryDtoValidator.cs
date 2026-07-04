using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class VoidJournalEntryDtoValidator : AbstractValidator<VoidJournalEntryDto>
{
    public VoidJournalEntryDtoValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("La razón de anulación no puede superar 500 caracteres.")
            .When(x => x.Reason is not null);

        RuleFor(x => x.VoidDate)
            .Must(d => !d.HasValue || d.Value <= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("La fecha de anulación no puede ser futura.");
    }
}
