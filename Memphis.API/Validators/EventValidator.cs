using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.API.Validators;

public class EventValidator : AbstractValidator<EventDto>
{
    public EventValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Host).NotEmpty();
    }
}
