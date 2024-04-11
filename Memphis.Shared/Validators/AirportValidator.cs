using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.Shared.Validators;

public class AirportValidator : AbstractValidator<AirportDto>
{
    public AirportValidator()
    {
        RuleFor(x => x.Icao).NotEmpty().Length(4);
        RuleFor(x => x.Name).NotEmpty();
    }
}
