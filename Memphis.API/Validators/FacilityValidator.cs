using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class FacilityValidator : AbstractValidator<FacilityPayload>
{
    public FacilityValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty();
    }
}
