using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class ValidateWebTemplateSyntax
{
    public record Query : IRequest<IQueryResponseDto<TemplateSyntaxResult>>
    {
        public string Content { get; init; } = string.Empty;
    }

    public record TemplateSyntaxResult(bool IsValid, string? Error, int? Line = null, int? Column = null);

    public class Handler : IRequestHandler<Query, IQueryResponseDto<TemplateSyntaxResult>>
    {
        private readonly ILiquidTemplateParser _parser;

        public Handler(ILiquidTemplateParser parser)
        {
            _parser = parser;
        }

        public ValueTask<IQueryResponseDto<TemplateSyntaxResult>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var error = _parser.GetSyntaxError(request.Content);
            return new ValueTask<IQueryResponseDto<TemplateSyntaxResult>>(
                new QueryResponseDto<TemplateSyntaxResult>(
                    error is null
                        ? new TemplateSyntaxResult(true, null)
                        : new TemplateSyntaxResult(false, error.Message, error.Line, error.Column)
                )
            );
        }
    }
}
