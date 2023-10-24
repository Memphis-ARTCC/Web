using FluentValidation;
using Memphis.Shared.Dtos;

namespace Memphis.API.Validators;

public class OtsValidator : AbstractValidator<OtsDto>
{
    public OtsValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.TrainingRequestId).NotEmpty();
        RuleFor(x => x.TrainingRequestId).NotEmpty();
    }
}
