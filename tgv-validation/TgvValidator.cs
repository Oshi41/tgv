using System;
using System.Linq.Expressions;
using FluentValidation;
using tgv_core.api;

namespace tgv_validation;

public class TgvValidator : AbstractValidator<Context>
{
    public TgvValidator ValidateFormField(string key, Action<IRuleBuilderInitial<Context, string>> builder) 
        => Validate(x => x.Form[key], builder);

    public TgvValidator Validate<T>(Expression<Func<Context, T>> expression, Action<IRuleBuilderInitial<Context, T>> builder)
    {
        var rule = RuleFor(expression);
        builder(rule);
        return this;
    }
}