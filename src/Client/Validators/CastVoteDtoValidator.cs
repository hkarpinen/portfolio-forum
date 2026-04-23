using Client.Contracts;
using FluentValidation;

namespace Client.Validators;

public sealed class CastVoteDtoValidator : AbstractValidator<CastVoteDto>
{
    public CastVoteDtoValidator()
    {
        RuleFor(x => x.TargetId)
            .NotEmpty();

        RuleFor(x => x.TargetType)
            .IsInEnum();

        RuleFor(x => x.Direction)
            .IsInEnum();
    }
}
