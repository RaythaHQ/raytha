using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Views.Commands;

public class EditFilter
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public IEnumerable<FilterConditionInputDto> Filter { get; init; } =
            new List<FilterConditionInputDto>();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, IRaythaDbJsonQueryEngine jsonQueryEngine)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db
                            .Views.Include(p => p.ContentType)
                            .FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("View", request.Id);

                        try
                        {
                            foreach (var filter in request.Filter)
                            {
                                if (
                                    FilterConditionType.From(filter.Type).DeveloperName
                                    == FilterConditionType.FilterCondition.DeveloperName
                                )
                                {
                                    if (string.IsNullOrEmpty(filter.Field))
                                    {
                                        context.AddFailure(
                                            Constants.VALIDATION_SUMMARY,
                                            $"Filter condition is missing a field choice"
                                        );
                                        return;
                                    }

                                    var conditionOperator = ConditionOperator.From(
                                        filter.ConditionOperator
                                    );
                                    if (
                                        !ConditionOperator.OperatorsWithoutValues.Contains(
                                            conditionOperator
                                        )
                                    )
                                    {
                                        if (string.IsNullOrEmpty(filter.Value))
                                        {
                                            context.AddFailure(
                                                Constants.VALIDATION_SUMMARY,
                                                $"Filter condition on field {filter.Field} is missing a value"
                                            );
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        catch (UnsupportedConditionOperatorException)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Filter conditions missing operators."
                            );
                            return;
                        }

                        try
                        {
                            var searchOnColumns =
                                entity.Columns != null ? entity.Columns.ToArray() : new string[0];
                            var viewFilter = request.Filter;
                            var conditionToODataUtility = new FilterConditionToODataUtility(
                                entity.ContentType
                            );

                            var filterConditions = new List<FilterCondition>();
                            foreach (var condition in request.Filter)
                            {
                                filterConditions.Add(
                                    new FilterCondition
                                    {
                                        Field = condition.Field,
                                        Value = condition.Value,
                                        ConditionOperator = string.IsNullOrEmpty(
                                            condition.ConditionOperator
                                        )
                                            ? null
                                            : ConditionOperator.From(condition.ConditionOperator),
                                        GroupOperator = string.IsNullOrEmpty(
                                            condition.GroupOperator
                                        )
                                            ? null
                                            : BooleanOperator.From(condition.GroupOperator),
                                        Type = FilterConditionType.From(condition.Type),
                                        Id = condition.Id,
                                        ParentId = condition.ParentId,
                                    }
                                );
                            }

                            var oDataFromFilter = conditionToODataUtility.ToODataFilter(
                                filterConditions
                            );
                            var queryResult = jsonQueryEngine.QueryContentItems(
                                entity.ContentTypeId,
                                searchOnColumns,
                                string.Empty,
                                new string[] { oDataFromFilter },
                                1,
                                1,
                                $"{BuiltInContentTypeField.PrimaryField.DeveloperName} {SortOrder.ASCENDING}"
                            );
                        }
                        catch (Exception ex)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "A filter value specified did not pass OData validation."
                            );
                            return;
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .Views.Include(p => p.ContentType)
                .ThenInclude(p => p.ContentTypeFields)
                .First(p => p.Id == request.Id.Guid);

            var contentTypeFields = entity.ContentType.ContentTypeFields;

            var filterConditions = new List<FilterCondition>();
            foreach (var condition in request.Filter)
            {
                filterConditions.Add(
                    new FilterCondition
                    {
                        Field = condition.Field,
                        Value = condition.Value,
                        ConditionOperator = string.IsNullOrEmpty(condition.ConditionOperator)
                            ? null
                            : ConditionOperator.From(condition.ConditionOperator),
                        GroupOperator = string.IsNullOrEmpty(condition.GroupOperator)
                            ? null
                            : BooleanOperator.From(condition.GroupOperator),
                        Type = FilterConditionType.From(condition.Type),
                        Id = condition.Id,
                        ParentId = condition.ParentId,
                    }
                );
            }

            entity.Filter = filterConditions;

            _db.Views.Update(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
