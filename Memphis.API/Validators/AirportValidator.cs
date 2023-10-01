using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class AirportValidator : AbstractValidator<Airport>
{
    public AirportValidator()
    {
        RuleFor(x => x.Icao).NotEmpty().Length(4);
        RuleFor(x => x.Name).NotEmpty();
    }
}
