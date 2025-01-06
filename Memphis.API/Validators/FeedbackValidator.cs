using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class FeedbackValidator : AbstractValidator<FeedbackPayload>
{
    public FeedbackValidator()
    {
        RuleFor(x => x.ControllerId).NotEmpty().GreaterThan(0);
        RuleFor(x => x.ControllerCallsign).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Level).NotEmpty();
    }
}
