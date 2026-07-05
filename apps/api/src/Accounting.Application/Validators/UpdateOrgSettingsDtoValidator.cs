using Accounting.Application.DTOs;
using Accounting.Domain.Enums;
using FluentValidation;

namespace Accounting.Application.Validators;

public class UpdateOrgSettingsDtoValidator : AbstractValidator<UpdateOrgSettingsDto>
{
    public UpdateOrgSettingsDtoValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("El nombre de la empresa es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("La URL del logo no puede superar 500 caracteres.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
                         && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp))
            .WithMessage("La URL del logo debe comenzar con https:// o http://.")
            .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));

        RuleFor(x => x.Address)
            .MaximumLength(300).WithMessage("La dirección no puede superar 300 caracteres.")
            .When(x => x.Address is not null);

        RuleFor(x => x.TaxId)
            .MaximumLength(50).WithMessage("El NIT/RUC no puede superar 50 caracteres.")
            .When(x => x.TaxId is not null);

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("El teléfono no puede superar 50 caracteres.")
            .When(x => x.Phone is not null);

        RuleFor(x => x.Email)
            .MaximumLength(200).WithMessage("El email no puede superar 200 caracteres.")
            .EmailAddress().WithMessage("El email no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.CurrencySymbol)
            .NotEmpty().WithMessage("El símbolo de moneda es requerido.")
            .MaximumLength(10).WithMessage("El símbolo de moneda no puede superar 10 caracteres.");

        RuleFor(x => x.Theme)
            .IsInEnum().WithMessage("El tema visual no es válido.");
    }
}
