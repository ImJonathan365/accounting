using Accounting.Application.DTOs;
using FluentValidation;

namespace Accounting.Application.Validators;

public class InviteMemberDtoValidator : AbstractValidator<InviteMemberDto>
{
    private static readonly string[] AllowedRoles = ["admin", "member"];

    public InviteMemberDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no es válido.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es requerido.")
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage("El rol debe ser 'admin' o 'member'.");
    }
}

public class UpdateMemberRoleDtoValidator : AbstractValidator<UpdateMemberRoleDto>
{
    private static readonly string[] AllowedRoles = ["admin", "member"];

    public UpdateMemberRoleDtoValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es requerido.")
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage("El rol debe ser 'admin' o 'member'.");
    }
}
