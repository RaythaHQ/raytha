using FluentValidation.Results;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// Maps Application-layer command/query responses onto HTTP results for the
/// cookie-authenticated admin JSON API. Success bodies are the DTO itself (or an
/// <c>{ id }</c> envelope for commands); failures are RFC 7807 validation problems
/// so the SPA's <c>formatError</c> can flatten <c>errors</c>.
/// </summary>
public static class AdminResults
{
    /// <summary>Key used in <c>errors</c> for form-level failures (Raytha's <c>__ValidationSummary</c>).</summary>
    public const string SummaryKey = "";

    public static IResult From<T>(IQueryResponseDto<T> response)
    {
        return response.Success ? Results.Ok(response.Result) : Problem(response.GetErrors());
    }

    public static IResult From<T, TOut>(IQueryResponseDto<T> response, Func<T, TOut> map)
    {
        return response.Success ? Results.Ok(map(response.Result)) : Problem(response.GetErrors());
    }

    public static IResult Paged<T>(
        IQueryResponseDto<ListResultDto<T>> response,
        PagedQuery paging
    )
        where T : class
    {
        if (!response.Success)
        {
            return Problem(response.GetErrors());
        }

        return Results.Ok(
            new PagedResponse<T>(
                response.Result.Items,
                response.Result.TotalCount,
                paging.PageNumber,
                paging.PageSize
            )
        );
    }

    public static IResult Paged<T, TOut>(
        IQueryResponseDto<ListResultDto<T>> response,
        PagedQuery paging,
        Func<T, TOut> map
    )
        where T : class
    {
        if (!response.Success)
        {
            return Problem(response.GetErrors());
        }

        return Results.Ok(
            new PagedResponse<TOut>(
                response.Result.Items.Select(map).ToList(),
                response.Result.TotalCount,
                paging.PageNumber,
                paging.PageSize
            )
        );
    }

    /// <summary>Command that yields an entity id: 200 <c>{ id }</c>, or 201 when <paramref name="created"/>.</summary>
    public static IResult FromId<T>(ICommandResponseDto<T> response, bool created = false)
    {
        if (!response.Success)
        {
            return Problem(response.GetErrors());
        }

        var body = new IdResponse(response.Result?.ToString() ?? string.Empty);
        return created ? Results.Created((string?)null, body) : Results.Ok(body);
    }

    /// <summary>Command whose result should be returned as-is.</summary>
    public static IResult From<T>(ICommandResponseDto<T> response)
    {
        return response.Success ? Results.Ok(response.Result) : Problem(response.GetErrors());
    }

    /// <summary>Command with no meaningful result: 204 on success.</summary>
    public static IResult NoContent<T>(ICommandResponseDto<T> response)
    {
        return response.Success ? Results.NoContent() : Problem(response.GetErrors());
    }

    public static IResult Problem(IEnumerable<ValidationFailure>? failures, int statusCode = 400)
    {
        var errors = new Dictionary<string, string[]>();
        string? detail = null;

        foreach (var group in (failures ?? []).GroupBy(f => f.PropertyName ?? string.Empty))
        {
            var key = group.Key == Constants.VALIDATION_SUMMARY ? SummaryKey : group.Key;
            var messages = group.Select(f => f.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).ToArray();
            if (key == SummaryKey && detail is null)
            {
                detail = messages.FirstOrDefault();
            }
            errors[key] = errors.TryGetValue(key, out var existing)
                ? existing.Concat(messages).ToArray()
                : messages;
        }

        detail ??= errors.Values.SelectMany(v => v).FirstOrDefault();

        return Results.ValidationProblem(errors, detail: detail, statusCode: statusCode);
    }

    public static IResult Problem(string message, int statusCode = 400)
    {
        return Results.Problem(detail: message, statusCode: statusCode);
    }

    public static IResult NotImplemented(string feature)
    {
        return Results.Problem(
            title: "Not implemented",
            detail: $"{feature} is not available in this build yet.",
            statusCode: StatusCodes.Status501NotImplemented
        );
    }
}

public sealed record IdResponse(string Id);

public sealed record PagedResponse<T>(IEnumerable<T> Items, int TotalCount, int PageNumber, int PageSize);
