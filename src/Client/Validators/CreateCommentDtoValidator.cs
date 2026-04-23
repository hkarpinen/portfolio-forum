using Client.Contracts;
using FluentValidation;

namespace Client.Validators;

public sealed class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(x => x.ThreadId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(5000);
    }
}
