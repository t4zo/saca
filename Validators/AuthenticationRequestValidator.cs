﻿using FluentValidation;
using SACA.Models.Requests;

namespace SACA.Validators
{
    public class AuthenticationRequestValidator : AbstractValidator<AuthenticationRequest>
    {
        public AuthenticationRequestValidator()
        {
            RuleFor(x => x.Email).NotNull().NotEmpty().WithMessage("Email não informado")
                .EmailAddress().WithMessage("Endereço de email inválido");

            RuleFor(x => x.Password).NotEmpty().WithMessage("Senha não informada")
                .MinimumLength(6).WithMessage("Senha menor que 6 caracteres");
        }
    }
}