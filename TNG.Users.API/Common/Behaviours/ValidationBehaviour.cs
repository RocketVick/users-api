using FluentValidation;
using MediatR;
using ValidationException = TNG.Users.API.Common.Exceptions.ValidationException;

namespace TNG.Users.API.Common.Behaviours;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults =
            await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        if (validationResults.Any(r => !r.IsValid))
        {
            throw new ValidationException(validationResults.SelectMany(x => x.Errors));
        }

        return await next();
    }
}
