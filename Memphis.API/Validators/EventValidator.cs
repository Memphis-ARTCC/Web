using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class EventValidator : AbstractValidator<EventPayload>
{
    public EventValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Host).NotEmpty();
    }
}
