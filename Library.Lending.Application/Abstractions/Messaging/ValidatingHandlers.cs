using FluentValidation;
using FluentValidation.Results;

namespace Library.Lending.Application.Abstractions.Messaging;

internal sealed class ValidatingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        await RequestValidation.ThrowIfInvalidAsync(command, validators, cancellationToken);
        return await inner.HandleAsync(command, cancellationToken);
    }
}

internal sealed class ValidatingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        await RequestValidation.ThrowIfInvalidAsync(query, validators, cancellationToken);
        return await inner.HandleAsync(query, cancellationToken);
    }
}

internal static class RequestValidation
{
    public static async Task ThrowIfInvalidAsync<TRequest>(
        TRequest request,
        IEnumerable<IValidator<TRequest>> validators,
        CancellationToken cancellationToken)
    {
        List<ValidationFailure>? failures = null;
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                (failures ??= []).AddRange(result.Errors);
            }
        }

        if (failures is { Count: > 0 })
        {
            throw new ValidationException(failures);
        }
    }
}
