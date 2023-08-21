using CSharpVitamins;
using Csv;
using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.MediaItems;
using Raytha.Application.Templates.Web.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.Events;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Raytha.Application.ContentItems.Commands
{
    public class BeginImportContentItemsFromCsv
    {
        public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
        {
            public ShortGuid ViewId { get; init; }
            public string ImportMethod { get; init; }

            public bool ImportAsDraft { get; init; }
            public byte[] CsvAsBytes { get; init; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator(IRaythaDbContext db)
            {
                RuleFor(x => x).Custom((request, context) =>
                {
                    if (string.IsNullOrEmpty(request.ImportMethod))
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, "Select which content items to import.");
                        return;
                    }
                    if (request.CsvAsBytes == null)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, "Please upload a file to import.");
                    }
                    if (request.ViewId == ShortGuid.Empty)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, "ViewId is required.");

                    }
                });
            }
        }

        public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
        {
            private readonly IBackgroundTaskQueue _taskQueue;
            private readonly IRaythaDbContext _entityFrameworkDb;
            private readonly IContentTypeInRoutePath _contentTypeInRoutePath;
            public Handler(
                IBackgroundTaskQueue taskQueue,
                IRaythaDbContext entityFrameworkDb,
                IContentTypeInRoutePath contentTypeInRoutePath
                )
            {
                _taskQueue = taskQueue;
                _entityFrameworkDb = entityFrameworkDb;
                _contentTypeInRoutePath = contentTypeInRoutePath;
            }
            public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
            {
                View view = _entityFrameworkDb.Views
                    .Include(p => p.ContentType)
                    .ThenInclude(p => p.ContentTypeFields)
                    .FirstOrDefault(p => p.Id == request.ViewId.Guid);

                if (view == null)
                    throw new NotFoundException("View", request.ViewId);

                _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(view.ContentType.DeveloperName);

                var backgroundJobId = await _taskQueue.EnqueueAsync<BackgroundTask>(request, cancellationToken);

                return new CommandResponseDto<ShortGuid>(backgroundJobId);
            }
        }

        public class BackgroundTask : IExecuteBackgroundTask
        {
            private readonly IRaythaDbJsonQueryEngine _db;
            private readonly IRaythaDbContext _entityFrameworkDb;
            private readonly ICSVService _csvService;

            public BackgroundTask(
                IRaythaDbJsonQueryEngine db,
                IRaythaDbContext entityFrameworkDb,
                ICSVService csvService)
            {
                _db = db;
                _entityFrameworkDb = entityFrameworkDb;
                _csvService = csvService;
            }
            public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
            {
                List<Dictionary<string, object>> records = new List<Dictionary<string, object>>();       
                List<Dictionary<string, object>> totalItems = new List<Dictionary<string, object>>();
                int itemCount = 0;

                Guid viewId = args.GetProperty("ViewId").GetProperty("Guid").GetGuid();
                string importMethod = args.GetProperty("ImportMethod").GetString();
                bool importAsDraft = args.GetProperty("ImportAsDraft").GetBoolean();
                byte[] csvAsBytes = args.GetProperty("CsvAsBytes").GetBytesFromBase64();

                View view = _entityFrameworkDb.Views
                    .Include(p => p.ContentType)
                    .ThenInclude(p => p.ContentTypeFields)
                    .FirstOrDefault(p => p.Id == viewId);

                if (view == null)
                    throw new NotFoundException("View", viewId);

                var job = _entityFrameworkDb.BackgroundTasks.FirstOrDefault(p => p.Id == jobId);
                job.TaskStep = 1;
                job.StatusInfo = $"Pulling records from file...";
                job.PercentComplete = 0;
                _entityFrameworkDb.BackgroundTasks.Update(job);
                await _entityFrameworkDb.SaveChangesAsync(cancellationToken);

                Stream stream = new MemoryStream(csvAsBytes);
                records = _csvService.ReadCSV<Dictionary<string, object>>(stream);

                if (importMethod == "add new records only")
                {
                    foreach (var record in records)
                    {
                        if (record.TryGetValue("id", out var idValue) && string.IsNullOrEmpty(idValue?.ToString()))
                        {
                            totalItems.Add(record);
                        }
                    }

                    itemCount = totalItems.Count;
                }
                else if (importMethod == "update existing records only")
                {
                    foreach (var record in records)
                    {
                        if (record.TryGetValue("id", out var idValue) && !string.IsNullOrEmpty(idValue?.ToString()))
                        {
                            totalItems.Add(record);
                        }
                    }
                    itemCount = totalItems.Count;
                }
                else
                {
                    totalItems = records;
                    itemCount = records.Count;
                }
                job.TaskStep = 2;
                job.StatusInfo = $"Total records in file : {itemCount}";
                job.PercentComplete = 0;
                _entityFrameworkDb.BackgroundTasks.Update(job);
                await _entityFrameworkDb.SaveChangesAsync(cancellationToken);

                foreach (var item in totalItems)
                {
                    var newEntityId = Guid.NewGuid();
                    ContentItem entity = null;

                    var template = _entityFrameworkDb.WebTemplates.FirstOrDefault(s => s.DeveloperName == item["templateDeveloperName"]);

                    if (!string.IsNullOrEmpty(item["id"].ToString()))
                    {
                        entity = _entityFrameworkDb.ContentItems.First(s => s.Id.ToString() == item["id"].ToString());
                    }

                    var fieldValues = new Dictionary<string, dynamic>();
                    var fields = item.Keys.Where(s => s != "id" && s!= "templateDeveloperName").ToList();
                    foreach (var field in fields)
                    {
                        var contentTypeField = view.ContentType.ContentTypeFields.First(p => p.DeveloperName == field.ToDeveloperName());
                        if (contentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
                        {

                            Guid guid = (ShortGuid)item[field];
                            fieldValues.Add(field.ToDeveloperName(), guid);
                        }
                        else
                        {
                            fieldValues.Add(field.ToDeveloperName(), item[field]);
                        }
                    }

                    var contentTypeDefinition = _entityFrameworkDb.ContentTypes
             .Include(p => p.ContentTypeFields)
             .First(p => p.DeveloperName == view.DeveloperName.ToDeveloperName());

                    if (string.IsNullOrWhiteSpace(item["id"].ToString()))
                    {
                        var path = GetRoutePath(fieldValues, newEntityId, contentTypeDefinition.Id);

                        entity = new ContentItem
                        {
                            Id = newEntityId,
                            IsDraft = importAsDraft,
                            IsPublished = importAsDraft == false,
                            DraftContent = fieldValues,
                            PublishedContent = fieldValues,
                            WebTemplateId = template.Id,
                            ContentTypeId = contentTypeDefinition.Id,
                            Route = new Route
                            {
                                Path = path,
                                ContentItemId = newEntityId
                            }
                        };
                    }
                    else
                    {
                        entity.IsDraft = importAsDraft;
                        entity.IsPublished = importAsDraft == false;
                        entity.DraftContent = fieldValues;
                        entity.PublishedContent = fieldValues;
                        entity.WebTemplateId = template.Id;
                        entity.ContentTypeId = contentTypeDefinition.Id;

                    }
                    if (string.IsNullOrWhiteSpace(item["id"].ToString()))
                    {
                        _entityFrameworkDb.ContentItems.Add(entity);
                    }
                    else
                    {
                        _entityFrameworkDb.ContentItems.Update(entity);
                    }

                    await _entityFrameworkDb.SaveChangesAsync(cancellationToken);
                }

                job.TaskStep = 3;
                job.StatusInfo = $"Finished Importing.";
                job.PercentComplete = 100;
                _entityFrameworkDb.BackgroundTasks.Update(job);
                await _entityFrameworkDb.SaveChangesAsync(cancellationToken);

            }

            private string GetRoutePath(dynamic content, Guid entityId, Guid contentTypeId)
            {
                var contentType = _entityFrameworkDb.ContentTypes
                    .Include(p => p.ContentTypeFields)
                    .First(p => p.Id == contentTypeId);

                var routePathTemplate = contentType.DefaultRouteTemplate;

                string primaryFieldDeveloperName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName;
                var primaryField = ((IDictionary<string, dynamic>)content)[primaryFieldDeveloperName] as string;

                string path = routePathTemplate.IfNullOrEmpty($"{BuiltInContentTypeField.PrimaryField.DeveloperName}")
                                               .Replace($"{{{BuiltInContentTypeField.PrimaryField.DeveloperName}}}", primaryField.IfNullOrEmpty((ShortGuid)entityId))
                                               .Replace($"{{{BuiltInContentTypeField.Id.DeveloperName}}}", (ShortGuid)entityId)
                                               .Replace("{ContentTypeDeveloperName}", contentType.DeveloperName)
                                               .Replace("{CurrentYear}", DateTime.UtcNow.Year.ToString())
                                               .Replace("{CurrentMonth}", DateTime.UtcNow.Month.ToString());

                path = path.ToUrlSlug().Truncate(200, string.Empty);

                if (_entityFrameworkDb.Routes.Any(p => p.Path == path))
                {
                    path = $"{(ShortGuid)entityId}-{path}".Truncate(200, string.Empty);
                }

                return path;
            }
           
        }
    }
}
