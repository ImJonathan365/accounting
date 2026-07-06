using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class UpdateAccountDtoValidator : AbstractValidator<UpdateAccountDto>
{
    public UpdateAccountDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.CashFlowSection)
            .IsInEnum().WithMessage("La sección de flujo de efectivo no es válida.");
    }
}
