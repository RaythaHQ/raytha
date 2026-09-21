using Mediator;
using Raytha.Application.Common.Models;

namespace Raytha.Application.FeatureFlags.Queries;

public class GetFeatureFlags
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyList<FeatureFlagDto>>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyList<FeatureFlagDto>>>
    {
        private readonly IFeatureFlagService _featureFlags;

        public Handler(IFeatureFlagService featureFlags)
        {
            _featureFlags = featureFlags;
        }

        public async ValueTask<IQueryResponseDto<IReadOnlyList<FeatureFlagDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var resolved = await _featureFlags.ListAsync(cancellationToken);
            IReadOnlyList<FeatureFlagDto> items = resolved.Select(FeatureFlagDto.FromResolved).ToList();
            return new QueryResponseDto<IReadOnlyList<FeatureFlagDto>>(items);
        }
    }
}
