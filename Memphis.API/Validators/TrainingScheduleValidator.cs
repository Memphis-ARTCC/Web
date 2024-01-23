using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.API.Validators;

public class TrainingScheduleValidator : AbstractValidator<TrainingScheduleDto>
{
    public TrainingScheduleValidator()
    {
        RuleFor(x => x.TrainingTypes).NotEmpty();
        RuleFor(x => x.Start).NotEmpty();
    }
}