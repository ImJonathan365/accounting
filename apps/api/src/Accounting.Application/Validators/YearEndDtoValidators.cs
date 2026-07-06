using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class YearEndCloseRequestDtoValidator : AbstractValidator<YearEndCloseRequestDto>
{
    public YearEndCloseRequestDtoValidator()
    {
        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000).WithMessage("El año debe ser 2000 o posterior.")
            .LessThanOrEqualTo(DateTime.UtcNow.Year).WithMessage("Solo se pueden cerrar años ya transcurridos.");

        RuleFor(x => x.RetainedEarningsAccountId)
            .NotEmpty().WithMessage("Debe seleccionar una cuenta de resultados acumulados.");
    }
}
