using MediatR;

namespace TNG.Users.API.Common.Behaviours;

public class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        TResponse response;
        var requestId = Guid.NewGuid();
        try
        {
            logger.LogInformation(MessageBuilder(request, requestId,"Start request: {request}"), request);
            response = await next();
        }
        catch (Exception e)
        {
            logger.LogError(e, MessageBuilder(request, requestId, "Unhandled Exception"));
            throw;
        }
        logger.LogInformation(MessageBuilder(request, requestId,"Request processed"));
        
        return response;
    }
    
    private string MessageBuilder(TRequest request, Guid id, string message) => $"[{id}] Request {typeof(TRequest).FullName};\n{message};";
}