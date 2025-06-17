using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class ExamRequestValidator : AbstractValidator<ExamRequestPayload>
{
    public ExamRequestValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
    }
}
