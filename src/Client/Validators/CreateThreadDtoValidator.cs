using Forum.Application.Commands;
using FluentValidation;

namespace Client.Validators;

public sealed class CreateThreadCommandValidator : AbstractValidator<CreateThreadCommand>
{
    public CreateThreadCommandValidator()
    {
        RuleFor(x => x.CommunitySlug)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Content)
            .MaximumLength(10000);
    }
}
