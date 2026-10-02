using System.Text.Json;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Attributes;
using Raytha.Application.Common.Behaviors;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.Webhooks;
using Raytha.Domain.JsonConverters;
using Raytha.Domain.ValueObjects.FieldTypes;
using static Raytha.Application.ContentItems.ContentItemBatchPlanner;

namespace Raytha.Application.ContentItems.Commands;

/// <summary>
/// Queues the creation of many content items of one content type and returns the background task
/// id to poll. Each item is created exactly as <see cref="CreateContentItem"/> would create it, so
/// one item failing does not stop the others. A relationship field may hold an item id, a route
/// path, or the primary field value of the related item, and an item may reference another item
/// of the same batch by its primary field value: the batch is ordered so that item is created
/// first. When the task completes, its status info holds a <see cref="BatchResult"/> as JSON.
/// </summary>
public class BeginBatchCreateContentItems
{
    public record BatchItem
    {
        /// <summary>Overrides the batch template for this item.</summary>
        public ShortGuid TemplateId { get; init; } = ShortGuid.Empty;
        public bool SaveAsDraft { get; init; }
        public IDictionary<string, dynamic> Content { get; init; } =
            new Dictionary<string, dynamic>();
    }

    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        [ExcludePropertyFromOpenApiDocs]
        public string ContentTypeDeveloperName { get; init; } = string.Empty;

        /// <summary>The template for every item that does not name its own.</summary>
        public ShortGuid TemplateId { get; init; } = ShortGuid.Empty;
        public IReadOnlyList<BatchItem> Items { get; init; } = [];
    }

    public record ItemResult(int Index, bool Success, string? Id, IReadOnlyList<ItemError> Errors);

    public record BatchResult(int Total, int Created, int Failed, IReadOnlyList<ItemResult> Items);

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (string.IsNullOrEmpty(request.ContentTypeDeveloperName))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "ContentTypeDeveloperName is required."
                            );
                            return;
                        }

                        var developerName = request.ContentTypeDeveloperName.ToDeveloperName();
                        if (!db.ContentTypes.Any(ct => ct.DeveloperName == developerName))
                        {
                            throw new NotFoundException("Content Type", developerName);
                        }

                        if (request.Items is null || request.Items.Count == 0)
                        {
                            context.AddFailure("Items", "Provide at least one item.");
                            return;
                        }

                        if (request.Items.Count > MaxBatchSize)
                        {
                            context.AddFailure(
                                "Items",
                                $"A batch holds at most {MaxBatchSize} items; this one has {request.Items.Count}."
                            );
                            return;
                        }

                        for (var index = 0; index < request.Items.Count; index++)
                        {
                            var item = request.Items[index];
                            if (item?.Content is null)
                            {
                                context.AddFailure($"Items[{index}].Content", "Content is required.");
                            }

                            if (
                                request.TemplateId == ShortGuid.Empty
                                && (item is null || item.TemplateId == ShortGuid.Empty)
                            )
                            {
                                context.AddFailure(
                                    $"Items[{index}].TemplateId",
                                    "TemplateId is required, on the item or on the batch."
                                );
                            }
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IBackgroundTaskQueue _taskQueue;

        public Handler(IBackgroundTaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var backgroundJobId = await _taskQueue.EnqueueAsync<BackgroundTask>(
                request,
                cancellationToken
            );

            return new CommandResponseDto<ShortGuid>(backgroundJobId);
        }
    }

    /// <summary>
    /// Creates each item with the <see cref="CreateContentItem"/> validator and handler, then
    /// publishes its <c>content_item.created</c> webhook, so an item is validated, stored, and
    /// announced as a single create is. The pipeline is not used: a background task has no request,
    /// so no signed-in user or IP address for the audit log. The queued batch itself is audited
    /// with the user who sent it.
    /// </summary>
    public class BackgroundTask : IExecuteBackgroundTask
    {
        private static readonly JsonSerializerOptions ArgsOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ShortGuidConverter() },
        };

        private static readonly JsonSerializerOptions ResultOptions = new(JsonSerializerDefaults.Web);

        private readonly IRaythaDbContext _db;
        private readonly IRaythaDbJsonQueryEngine _jsonQueryEngine;
        private readonly IWebhookEventPublisher _webhookPublisher;
        private readonly ILogger<BackgroundTask> _logger;

        public BackgroundTask(
            IRaythaDbContext db,
            IRaythaDbJsonQueryEngine jsonQueryEngine,
            IWebhookEventPublisher webhookPublisher,
            ILogger<BackgroundTask> logger
        )
        {
            _db = db;
            _jsonQueryEngine = jsonQueryEngine;
            _webhookPublisher = webhookPublisher;
            _logger = logger;
        }

        public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            var command = args.Deserialize<Command>(ArgsOptions)!;
            var job = await _db.BackgroundTasks.FirstAsync(p => p.Id == jobId, cancellationToken);

            var developerName = command.ContentTypeDeveloperName.ToDeveloperName();
            var contentType = await _db
                .ContentTypes.Include(ct => ct.ContentTypeFields)
                .FirstAsync(ct => ct.DeveloperName == developerName, cancellationToken);
            var primaryField = contentType.ContentTypeFields.First(f =>
                f.Id == contentType.PrimaryFieldId
            );
            var relationshipFields = contentType
                .ContentTypeFields.Where(f =>
                    f.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship.DeveloperName
                    && f.RelatedContentTypeId.HasValue
                )
                .Select(f => new RelationshipField(
                    f.DeveloperName,
                    f.Label,
                    f.RelatedContentTypeId!.Value
                ))
                .ToList();

            var contents = command.Items.Select(i => i.Content).ToList();
            var references = CollectReferences(contents, relationshipFields);
            var lookups = await new ContentItemReferenceResolver(
                _db,
                _jsonQueryEngine
            ).ResolveAsync(references, cancellationToken);
            var plan = Plan(
                contentType.Id,
                primaryField.DeveloperName,
                primaryField.Label,
                contents,
                relationshipFields,
                lookups
            );

            var results = new ItemResult?[command.Items.Count];
            var created = new Dictionary<int, ShortGuid>();
            var processed = 0;

            foreach (var index in plan.Order)
            {
                results[index] = await CreateItem(
                    command,
                    developerName,
                    plan.Items[index],
                    created,
                    cancellationToken
                );
                processed++;

                job.StatusInfo = $"Processed {processed} of {command.Items.Count} items.";
                job.PercentComplete = Math.Clamp(processed * 100 / command.Items.Count, 10, 99);
            }

            job.StatusInfo = Serialize(results, plan);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<ItemResult> CreateItem(
            Command command,
            string contentTypeDeveloperName,
            ItemPlan itemPlan,
            Dictionary<int, ShortGuid> created,
            CancellationToken cancellationToken
        )
        {
            var index = itemPlan.Index;
            if (itemPlan.Errors.Count > 0)
            {
                return Failure(index, itemPlan.Errors);
            }

            var item = command.Items[index];
            var errors = new List<ItemError>();
            var content = BuildContent(itemPlan, item.Content, created, errors);
            if (errors.Count > 0)
            {
                return Failure(index, errors);
            }

            var createCommand = new CreateContentItem.Command
            {
                ContentTypeDeveloperName = contentTypeDeveloperName,
                TemplateId = item.TemplateId != ShortGuid.Empty ? item.TemplateId : command.TemplateId,
                SaveAsDraft = item.SaveAsDraft,
                Content = content,
            };

            try
            {
                var validation = await new CreateContentItem.Validator(_db).ValidateAsync(
                    createCommand,
                    cancellationToken
                );
                if (!validation.IsValid)
                {
                    return Failure(
                        index,
                        validation
                            .Errors.Select(e => new ItemError(
                                e.PropertyName == Constants.VALIDATION_SUMMARY
                                    ? null
                                    : e.PropertyName,
                                e.ErrorMessage
                            ))
                            .ToList()
                    );
                }

                var response = await new CreateContentItem.Handler(_db).Handle(
                    createCommand,
                    cancellationToken
                );
                created[index] = response.Result;

                await PublishCreated(createCommand, response, cancellationToken);
                return new ItemResult(index, true, response.Result.ToString(), []);
            }
            catch (NotFoundException ex)
            {
                return Failure(index, [new ItemError(null, ex.Message)]);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Batch create failed for item {Index}", index);
                DiscardPendingInserts();
                return Failure(index, [new ItemError(null, "The item could not be created.")]);
            }
        }

        private async Task PublishCreated(
            CreateContentItem.Command command,
            CommandResponseDto<ShortGuid> response,
            CancellationToken cancellationToken
        )
        {
            var attribute = WebhookEventCatalog.FindAttribute(typeof(CreateContentItem.Command));
            if (attribute is null)
            {
                return;
            }

            try
            {
                await _webhookPublisher.PublishAsync(
                    attribute.EventName,
                    WebhookPublishBehavior<
                        CreateContentItem.Command,
                        CommandResponseDto<ShortGuid>
                    >.BuildPayload(command, response),
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Webhook publish failed for {EventName}", attribute.EventName);
            }
        }

        /// <summary>
        /// The queue worker shares one database context across tasks, so a failed insert left
        /// tracked would be retried by its next save and fail again, taking the worker down.
        /// </summary>
        private void DiscardPendingInserts()
        {
            if (_db is not DbContext context)
            {
                return;
            }

            foreach (
                var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList()
            )
            {
                entry.State = EntityState.Detached;
            }
        }

        private static ItemResult Failure(int index, IReadOnlyList<ItemError> errors) =>
            new(index, false, null, errors);

        private static string Serialize(ItemResult?[] results, BatchPlan plan)
        {
            var items = results
                .Select(
                    (result, index) =>
                        result
                        ?? Failure(
                            index,
                            plan.Items[index].Errors.Count > 0
                                ? plan.Items[index].Errors.ToList()
                                : [new ItemError(null, "The item was not created.")]
                        )
                )
                .ToList();
            var created = items.Count(i => i.Success);

            return JsonSerializer.Serialize(
                new BatchResult(items.Count, created, items.Count - created, items),
                ResultOptions
            );
        }
    }
}
