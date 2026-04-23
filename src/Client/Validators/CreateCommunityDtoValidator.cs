using Client.Contracts;
using FluentValidation;

namespace Client.Validators;

public sealed class CreateCommunityDtoValidator : AbstractValidator<CreateCommunityDto>
{
    public CreateCommunityDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Visibility)
            .IsInEnum();
    }
}
