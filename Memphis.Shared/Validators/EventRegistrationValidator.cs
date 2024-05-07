using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.Shared.Validators;

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
