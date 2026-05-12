using Forum.Application.Commands;
using FluentValidation;

namespace Client.Validators;

public sealed class CreateCommunityCommandValidator : AbstractValidator<CreateCommunityCommand>
{
    public CreateCommunityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Visibility)
            .IsInEnum();
    }
}
