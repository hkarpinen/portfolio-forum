using Client.Contracts;
using FluentValidation;

namespace Client.Validators;

public sealed class CreateThreadDtoValidator : AbstractValidator<CreateThreadDto>
{
    public CreateThreadDtoValidator()
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
