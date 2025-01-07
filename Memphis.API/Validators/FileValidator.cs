using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class FileValidator : AbstractValidator<FilePayload>
{
    public FileValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Version).NotEmpty();
        RuleFor(x => x.Type).NotEmpty();
    }
}
