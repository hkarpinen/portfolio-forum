using Forum.Application.Commands;
using FluentValidation;

namespace Client.Validators;

public sealed class SearchQueryCommandValidator : AbstractValidator<SearchQueryCommand>
{
    public SearchQueryCommandValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Scope)
            .IsInEnum();

        RuleFor(x => x.Sort)
            .IsInEnum();

        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
