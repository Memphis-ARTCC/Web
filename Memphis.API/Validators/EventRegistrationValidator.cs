using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.API.Validators;

public class EventRegistrationValidator : AbstractValidator<EventRegistrationDto>
{
    public EventRegistrationValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.EventPositionId).NotEmpty();
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.End).NotEmpty();
    }
}
