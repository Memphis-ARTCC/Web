using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.Shared.Validators;

public class FeedbackValidator : AbstractValidator<FeedbackDto>
{
    public FeedbackValidator()
    {
        RuleFor(x => x.ControllerId).NotEmpty().GreaterThan(0);
        RuleFor(x => x.ControllerCallsign).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Level).NotEmpty();
    }
}
