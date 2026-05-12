using Forum.Application.Commands;
using FluentValidation;

namespace Client.Validators;

public sealed class CastVoteCommandValidator : AbstractValidator<CastVoteCommand>
{
    public CastVoteCommandValidator()
    {
        RuleFor(x => x.TargetId)
            .NotEmpty();

        RuleFor(x => x.TargetType)
            .IsInEnum();

        RuleFor(x => x.Direction)
            .IsInEnum();
    }
}
