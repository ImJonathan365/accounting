using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class CreateAccountDtoValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es requerido.")
            .MaximumLength(20).WithMessage("El código no puede superar 20 caracteres.")
            .Matches(@"^[\w.\-]+$").WithMessage("El código solo puede contener letras, números, puntos y guiones.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("El tipo de cuenta no es válido.");
    }
}
