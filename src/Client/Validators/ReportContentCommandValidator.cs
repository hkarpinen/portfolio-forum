using FluentValidation;
using Forum.Application.Commands;

namespace Client.Validators;

public sealed class ReportContentCommandValidator : AbstractValidator<ReportContentCommand>
{
    public ReportContentCommandValidator()
    {
        RuleFor(x => x.CommunityId)
            .NotEmpty();

        RuleFor(x => x.TargetId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Details)
            .MaximumLength(2000);
    }
}
