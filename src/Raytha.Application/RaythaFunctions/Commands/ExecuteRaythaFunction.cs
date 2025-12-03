using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.RaythaFunctions.Commands;

public class ExecuteRaythaFunction
{
    public record Command : IRequest<CommandResponseDto<object>>
    {
        public required string DeveloperName { get; init; }
        public required string RequestMethod { get; init; }
        public required string QueryJson { get; init; }
        public required string PayloadJson { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.RequestMethod).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var developerName = request.DeveloperName.ToDeveloperName(allowDot: true);
                        var raythaFunction = db
                            .RaythaFunctions.Where(rf => rf.DeveloperName == developerName)
                            .Select(rf => new { rf.IsActive, rf.TriggerType })
                            .FirstOrDefault();

                        if (raythaFunction == null || !raythaFunction.IsActive)
                        {
                            // Security: Prevents invoking disabled or non-existent Raytha functions over HTTP,
                            // which could otherwise allow unauthenticated code execution if the function were later reactivated.
                            context.AddFailure(
                                "IsActive",
                                $"A function with the developer name {developerName} does not exist."
                            );
                        }
                        else if (
                            raythaFunction.TriggerType.DeveloperName
                            != RaythaFunctionTriggerType.HttpRequest.DeveloperName
                        )
                        {
                            // Security: Ensures only functions explicitly configured as HttpRequest triggers
                            // are callable via the public HTTP endpoint, reducing the risk of abusing internal
                            // event-driven functions as unintended webhooks; this is safe because HttpRequest
                            // is already the documented trigger type for HTTP-exposed functions.
                            context.AddFailure(
                                "TriggerType",
                                "This function is not configured to be triggered via HTTP request."
                            );
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<object>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IRaythaFunctionConfiguration _raythaFunctionConfiguration;
        private readonly IRaythaFunctionScriptEngine _raythaFunctionScriptEngine;
        private readonly IRaythaFunctionSemaphore _raythaFunctionSemaphore;

        public Handler(
            IRaythaDbContext db,
            IRaythaFunctionConfiguration raythaFunctionConfiguration,
            IRaythaFunctionSemaphore raythaFunctionSemaphore,
            IRaythaFunctionScriptEngine raythaFunctionScriptEngine
        )
        {
            _db = db;
            _raythaFunctionConfiguration = raythaFunctionConfiguration;
            _raythaFunctionSemaphore = raythaFunctionSemaphore;
            _raythaFunctionScriptEngine = raythaFunctionScriptEngine;
        }

        public async ValueTask<CommandResponseDto<object>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var developerName = request.DeveloperName.ToDeveloperName(allowDot: true);
            var code = await _db
                .RaythaFunctions.Where(rf =>
                    rf.DeveloperName == developerName
                    && rf.TriggerType == RaythaFunctionTriggerType.HttpRequest
                )
                .Select(rf => rf.Code)
                .FirstAsync(cancellationToken);
            // Security: Handler-level guard mirrors the validator so that even if validation is skipped
            // or bypassed, only HttpRequest-trigger functions can be executed via the HTTP entry point,
            // which narrows the attack surface without changing behavior for correctly configured functions.

            if (
                await _raythaFunctionSemaphore.WaitAsync(
                    _raythaFunctionConfiguration.QueueTimeout,
                    cancellationToken
                )
            )
            {
                try
                {
                    return request.RequestMethod switch
                    {
                        "GET" => new CommandResponseDto<object>(
                            await _raythaFunctionScriptEngine.EvaluateGet(
                                code,
                                request.QueryJson,
                                _raythaFunctionConfiguration.ExecuteTimeout,
                                cancellationToken
                            )
                        ),
                        "POST" => new CommandResponseDto<object>(
                            await _raythaFunctionScriptEngine.EvaluatePost(
                                code,
                                request.PayloadJson,
                                request.QueryJson,
                                _raythaFunctionConfiguration.ExecuteTimeout,
                                cancellationToken
                            )
                        ),
                        _ => throw new NotImplementedException(),
                    };
                }
                catch (Exception exception)
                    when (exception
                            is RaythaFunctionExecuteTimeoutException
                                or RaythaFunctionScriptException
                    )
                {
                    return new CommandResponseDto<object>("Function", exception.Message);
                }
                finally
                {
                    _raythaFunctionSemaphore.Release();
                }
            }
            else
            {
                return new CommandResponseDto<object>(
                    "Queue of functions",
                    "The server is too busy to handle your request. Please wait a few minutes and try again."
                );
            }
        }
    }
}
