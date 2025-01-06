using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class EventPositionValidator : AbstractValidator<EventPositionPayload>
{
    public EventPositionValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.MinRating).NotEmpty();
    }
}
