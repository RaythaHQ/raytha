using Mediator;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Exceptions;

namespace Raytha.Application.Common.Behaviors;

public class UnhandledExceptionBehaviour<TMessage, TResponse>
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<TMessage> _logger;

    public UnhandledExceptionBehaviour(ILogger<TMessage> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await next(message, cancellationToken);
        }
        catch (NotFoundException ex)
        {
            _logger.LogInformation($"Entity Not Found: {ex.Message}");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogInformation($"Unauthorized Access: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            var messageName = typeof(TMessage).Name;

            // Security: Avoid logging entire request payloads, which may contain credentials, tokens,
            // or other sensitive fields; instead log only the request name alongside the exception to
            // retain diagnostic value without increasing the risk of sensitive data exposure in logs.
            _logger.LogError(
                ex,
                "Raytha Request: Unhandled Exception for Request {Name}",
                messageName
            );

            throw;
        }
    }
}
