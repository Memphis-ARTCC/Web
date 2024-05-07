using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class TrainingMilestoneValidator : AbstractValidator<TrainingMilestone>
{
    public TrainingMilestoneValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Facility).NotEmpty();
    }
}