using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using System.Text.Json;

namespace Raytha.Application.ContentItems.Commands
{
    public class BeginImportContentItemsFromCsv
    {
        public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
        {
            public ShortGuid ContentTypeId { get; init; }
            public string ImportMethod { get; init; }
            public bool ImportAsDraft { get; init; }
            public byte[] CsvAsBytes { get; init; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator(IRaythaDbContext db, ICSVService csvService)
            {
                RuleFor(x => x).Custom((request, context) =>
                {
                    var contentType = db.ContentTypes
                        .Include(p => p.ContentTypeFields)
                        .FirstOrDefault(p => p.Id == request.ContentTypeId.Guid);

                    if (contentType == null)
                    {
                        throw new NotFoundException("Content Type", request.ContentTypeId);
                    }

                    if (string.IsNullOrEmpty(request.ImportMethod))
                    {
                        context.AddFailure("ImportMethod", "Import method is required.");
                        return;
                    }
                    if (request.CsvAsBytes == null || request.CsvAsBytes.Length == 0)
                    {
                        context.AddFailure("CsvAsBytes", "You must upload a CSV file.");
                        return;
                    }
                    else
                    {
                        try
                        {
                            Stream stream = new MemoryStream(request.CsvAsBytes);
                            var csvFile = csvService.ReadCSV<Dictionary<string, dynamic>>(stream);
                            if (!csvFile.Any())
                            {
                                context.AddFailure(Constants.VALIDATION_SUMMARY, "Your CSV file is missing data.");
                                return;
                            }
                            if (!csvFile.All(p => p.ContainsKey(BuiltInContentTypeField.Template.DeveloperName))) 
                            {
                                context.AddFailure(Constants.VALIDATION_SUMMARY, $"You must provide `{BuiltInContentTypeField.Template.DeveloperName}` column.");
                                return;
                            }
                            if (request.ImportMethod == "update existing records only" && !csvFile.All(p => p.ContainsKey(BuiltInContentTypeField.Id.DeveloperName)))
                            {
                                context.AddFailure(Constants.VALIDATION_SUMMARY, $"You must provide `{BuiltInContentTypeField.Id.DeveloperName}` column for updating records.");
                                return;
                            }
                        } 
                        catch (Exception ex) 
                        {
                            context.AddFailure(Constants.VALIDATION_SUMMARY, $"There was an error processing your CSV file: {ex.Message}");
                            return;
                        }
                    }
                });
            }
        }

        public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
        {
            private readonly IBackgroundTaskQueue _taskQueue;
            private readonly IRaythaDbContext _db;
            private readonly IContentTypeInRoutePath _contentTypeInRoutePath;
            public Handler(
                IBackgroundTaskQueue taskQueue,
                IRaythaDbContext db,
                IContentTypeInRoutePath contentTypeInRoutePath
                )
            {
                _taskQueue = taskQueue;
                _db = db;
                _contentTypeInRoutePath = contentTypeInRoutePath;
            }
            public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
            {
                var contentType = _db.ContentTypes
                    .Include(p => p.ContentTypeFields)
                    .First(p => p.Id == request.ContentTypeId.Guid);

                _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(contentType.DeveloperName);

                var backgroundJobId = await _taskQueue.EnqueueAsync<BackgroundTask>(request, cancellationToken);

                return new CommandResponseDto<ShortGuid>(backgroundJobId);
            }
        }

        public class BackgroundTask : IExecuteBackgroundTask
        {
            private readonly IRaythaDbContext _db;
            private readonly ICSVService _csvService;

            public BackgroundTask(
                IRaythaDbContext db,
                ICSVService csvService)
            {
                _db = db;
                _csvService = csvService;
            }
            public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
            {
                Guid contentTypeId = args.GetProperty("ContentTypeId").GetProperty("Guid").GetGuid();
                string importMethod = args.GetProperty("ImportMethod").GetString();
                bool importAsDraft = args.GetProperty("ImportAsDraft").GetBoolean();
                byte[] csvAsBytes = args.GetProperty("CsvAsBytes").GetBytesFromBase64();

                Stream stream = new MemoryStream(csvAsBytes);
                var records = _csvService.ReadCSV<Dictionary<string, dynamic>>(stream);

                ContentType contentType = _db.ContentTypes
                    .Include(p => p.ContentTypeFields)
                    .First(p => p.Id == contentTypeId);

                var job = _db.BackgroundTasks.FirstOrDefault(p => p.Id == jobId);
                job.TaskStep = 1;
                job.StatusInfo = $"Pulling records from file...";
                job.PercentComplete = 10;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                job.TaskStep = 2;
                job.StatusInfo = $"Number of records to process: {records.Count}";
                job.PercentComplete = 30;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                Dictionary<string, string> errorList = new Dictionary<string, string>();
                int rowNumber = 1;
                foreach (var item in records)
                {
                    var template = _db.WebTemplates.FirstOrDefault(s => s.DeveloperName == item[BuiltInContentTypeField.Template.DeveloperName] as string);
                    if (template == null)
                    {
                        errorList.Add($"Row number: {rowNumber}", $"Template developer name is was not found: {item[BuiltInContentTypeField.Template.DeveloperName] as string}");
                        rowNumber++;
                        continue;
                    }

                    try
                    {
                        ShortGuid entityId;

                        bool hasId = item.ContainsKey(BuiltInContentTypeField.Id.DeveloperName) ? !string.IsNullOrEmpty(item[BuiltInContentTypeField.Id.DeveloperName]?.ToString()) : false;
                        if (hasId)
                        {
                            if (!ShortGuid.TryParse(item[BuiltInContentTypeField.Id.DeveloperName]?.ToString(), out entityId))
                            {
                                errorList.Add($"Row number: {rowNumber}", $"{item[BuiltInContentTypeField.Id.DeveloperName]?.ToString()} is not a valid Raytha Id value");
                                rowNumber++;
                                continue;
                            }

                            var entity = _db.ContentItems.FirstOrDefault(p => p.Id == entityId.Guid);
                            var fieldValues = GetFieldValuesFromRecord(contentType.ContentTypeFields, item);
                            if (entity != null)
                            {
                                if (importMethod == "add new records only")
                                {
                                    rowNumber++;
                                    continue;
                                }
                                else
                                {
                                    entity.IsDraft = importAsDraft;
                                    entity.IsPublished = importAsDraft == false;
                                    entity.DraftContent = fieldValues;
                                    entity.PublishedContent = fieldValues;
                                    entity.WebTemplateId = template.Id;
                                    entity.ContentTypeId = contentType.Id;
                                    _db.ContentItems.Update(entity);
                                }
                            }
                            else if (importMethod == "update existing records only")
                            {
                                rowNumber++;
                                continue;
                            }
                            else
                            {
                                var path = GetRoutePath(fieldValues, entityId, contentType);

                                entity = new ContentItem
                                {
                                    Id = entityId.Guid,
                                    IsDraft = importAsDraft,
                                    IsPublished = importAsDraft == false,
                                    DraftContent = fieldValues,
                                    PublishedContent = fieldValues,
                                    WebTemplateId = template.Id,
                                    ContentTypeId = contentType.Id,
                                    Route = new Route
                                    {
                                        Path = path,
                                        ContentItemId = entityId.Guid
                                    }
                                };
                                _db.ContentItems.Add(entity);
                            }
                        }
                        else
                        {
                            ShortGuid newEntityId = ShortGuid.NewGuid();
                            var fieldValues = GetFieldValuesFromRecord(contentType.ContentTypeFields, item);
                            var path = GetRoutePath(fieldValues, newEntityId, contentType);

                            var entity = new ContentItem
                            {
                                Id = newEntityId.Guid,
                                IsDraft = importAsDraft,
                                IsPublished = importAsDraft == false,
                                DraftContent = fieldValues,
                                PublishedContent = fieldValues,
                                WebTemplateId = template.Id,
                                ContentTypeId = contentType.Id,
                                Route = new Route
                                {
                                    Path = path,
                                    ContentItemId = newEntityId.Guid
                                }
                            };
                            _db.ContentItems.Add(entity);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorList.Add($"Row number: {rowNumber}", ex.Message);
                        rowNumber++;
                        continue;
                    }

                    rowNumber++;
                    await _db.SaveChangesAsync(cancellationToken);
                }

                job.TaskStep = 3;
                job.StatusInfo = $"Finished importing.";
                job.PercentComplete = 100;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

            }

            private string GetRoutePath(dynamic content, ShortGuid entityId, ContentType contentType)
            {
                var routePathTemplate = contentType.DefaultRouteTemplate;

                string primaryFieldDeveloperName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName;
                var primaryField = ((IDictionary<string, dynamic>)content)[primaryFieldDeveloperName] as string;

                string path = routePathTemplate.IfNullOrEmpty($"{BuiltInContentTypeField.PrimaryField.DeveloperName}")
                                               .Replace($"{{{BuiltInContentTypeField.PrimaryField.DeveloperName}}}", primaryField.IfNullOrEmpty(entityId))
                                               .Replace($"{{{BuiltInContentTypeField.Id.DeveloperName}}}", (ShortGuid)entityId)
                                               .Replace("{ContentTypeDeveloperName}", contentType.DeveloperName)
                                               .Replace("{CurrentYear}", DateTime.UtcNow.Year.ToString())
                                               .Replace("{CurrentMonth}", DateTime.UtcNow.Month.ToString());

                path = path.ToUrlSlug().Truncate(200, string.Empty);

                if (_db.Routes.Any(p => p.Path == path))
                {
                    path = $"{entityId}-{path}".Truncate(200, string.Empty);
                }

                return path;
            }

            private Dictionary<string, dynamic> GetFieldValuesFromRecord(IEnumerable<ContentTypeField> contentTypeFields, Dictionary<string, object> record)
            {
                var fieldValues = new Dictionary<string, dynamic>();
                var fields = record.Keys.Where(s => s != BuiltInContentTypeField.Id.DeveloperName && s != BuiltInContentTypeField.Template.DeveloperName).ToList();
                foreach (var field in fields)
                {
                    var contentTypeField = contentTypeFields.FirstOrDefault(p => p.DeveloperName == field.ToDeveloperName());
                    if (contentTypeField == null)
                        continue;

                    if (contentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
                    {
                        ShortGuid value = null;
                        ShortGuid.TryParse(record[field].ToString(), out value);
                        Guid guid = value;
                        fieldValues.Add(field.ToDeveloperName(), guid);
                    }
                    else if (contentTypeField.FieldType.DeveloperName == BaseFieldType.MultipleSelect)
                    {
                        fieldValues.Add(field.ToDeveloperName(), record[field].ToString().Split(';').ToArray());
                    }
                    else
                    {
                        fieldValues.Add(field.ToDeveloperName(), record[field]);
                    }
                }
                return fieldValues;
            }
        }
    }
}
