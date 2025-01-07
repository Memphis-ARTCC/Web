using FluentValidation;
using Memphis.Shared.Models;

namespace Memphis.API.Validators;

public class UserValidator : AbstractValidator<UserPayload>
{
    public UserValidator()
    {
    }
}
