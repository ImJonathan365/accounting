using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class ClosePeriodDtoValidator : AbstractValidator<ClosePeriodDto>
{
    public ClosePeriodDtoValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("El mes debe ser un valor entre 1 y 12.");

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000).WithMessage("El año no puede ser anterior a 2000.")
            .LessThanOrEqualTo(DateTime.UtcNow.Year + 1).WithMessage("El año no puede ser futuro.");
    }
}
