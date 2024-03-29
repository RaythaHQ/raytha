using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.Common.Exceptions;

namespace Raytha.Application.RaythaFunctions.Commands;

public class ExecuteRaythaFunction
{
    public record Command : LoggableRequest<CommandResponseDto<object>>
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
            RuleFor(x => x).Custom((request, context) =>
            {
                var raythaFunction = db.RaythaFunctions.Where(rf => rf.DeveloperName == request.DeveloperName.ToDeveloperName())
                    .Select(rf => new { rf.IsActive })
                    .FirstOrDefault();

                if (raythaFunction == null || !raythaFunction.IsActive)
                    context.AddFailure("IsActive", $"A function with the developer name {request.DeveloperName} do not exist.");
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<object>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IRaythaFunctionConfiguration _raythaFunctionConfiguration;
        private readonly IRaythaFunctionScriptEngine _raythaFunctionScriptEngine;
        private readonly IRaythaFunctionSemaphore _raythaFunctionSemaphore;

        public Handler(IRaythaDbContext db,
            IRaythaFunctionConfiguration raythaFunctionConfiguration,
            IRaythaFunctionSemaphore raythaFunctionSemaphore,
            IRaythaFunctionScriptEngine raythaFunctionScriptEngine)
        {
            _db = db;
            _raythaFunctionConfiguration = raythaFunctionConfiguration;
            _raythaFunctionSemaphore = raythaFunctionSemaphore;
            _raythaFunctionScriptEngine = raythaFunctionScriptEngine;
        }

        public async Task<CommandResponseDto<object>> Handle(Command request, CancellationToken cancellationToken)
        {
            var code = await _db.RaythaFunctions.Where(rf => rf.DeveloperName == request.DeveloperName.ToDeveloperName())
                .Select(rf => rf.Code)
                .FirstAsync(cancellationToken);

            if (await _raythaFunctionSemaphore.WaitAsync(_raythaFunctionConfiguration.QueueTimeout, cancellationToken))
            {
                try
                {
                    _raythaFunctionScriptEngine.Initialize(code);

                    return request.RequestMethod switch
                    {
                        "GET" => new CommandResponseDto<object>(await _raythaFunctionScriptEngine.EvaluateGet(request.QueryJson, _raythaFunctionConfiguration.ExecuteTimeout, cancellationToken)),
                        "POST" => new CommandResponseDto<object>(await _raythaFunctionScriptEngine.EvaluatePost(request.PayloadJson, request.QueryJson, _raythaFunctionConfiguration.ExecuteTimeout, cancellationToken)),
                        _ => throw new NotImplementedException(),
                    };
                }
                catch (Exception exception) when (exception is RaythaFunctionExecuteTimeoutException or RaythaFunctionScriptException)
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
                return new CommandResponseDto<object>("Queue of functions", "The server is too busy to handle your request. Please wait a few minutes and try again.");
            }
        }
    }
}