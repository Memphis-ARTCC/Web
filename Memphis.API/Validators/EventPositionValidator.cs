using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.API.Validators;

public class EventPositionValidator : AbstractValidator<EventPositionDto>
{
    public EventPositionValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.MinRating).NotEmpty();
    }
}
