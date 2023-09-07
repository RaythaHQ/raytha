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
        public const string UPDATE_EXISTING_RECORDS_ONLY = "update_existing_records_only";
        public const string UPSERT_ALL_RECORDS = "upsert_all_records";
        public const string ADD_NEW_RECORDS_ONLY = "add_new_records_only";
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

                    if (request.ImportMethod != UPDATE_EXISTING_RECORDS_ONLY &&
                        request.ImportMethod != ADD_NEW_RECORDS_ONLY &&
                        request.ImportMethod != UPSERT_ALL_RECORDS)
                    {
                        context.AddFailure("ImportMethod", "Import method not recognized.");
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
                            if (request.ImportMethod == UPDATE_EXISTING_RECORDS_ONLY && !csvFile.All(p => p.ContainsKey(BuiltInContentTypeField.Id.DeveloperName)))
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
            private readonly IFileStorageProvider _fileStorageProvider;
            private readonly ICurrentOrganization _currentOrganization;
            public BackgroundTask(
                IRaythaDbContext db,
                ICSVService csvService,
                IFileStorageProvider fileStorageProvider,
                ICurrentOrganization currentOrganization)
            {
                _db = db;
                _csvService = csvService;
                _fileStorageProvider = fileStorageProvider;
                _currentOrganization = currentOrganization;
            }
            public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
            {
                Guid contentTypeId = args.GetProperty("ContentTypeId").GetProperty("Guid").GetGuid();
                string importMethod = args.GetProperty("ImportMethod").GetString();
                bool importAsDraft = args.GetProperty("ImportAsDraft").GetBoolean();
                byte[] csvAsBytes = args.GetProperty("CsvAsBytes").GetBytesFromBase64();

                int taskStep = 0; 
                Stream stream = new MemoryStream(csvAsBytes);

                var records = _csvService.ReadCSV<Dictionary<string, dynamic>>(stream);
                ContentType contentType = _db.ContentTypes
                                     .Include(p => p.ContentTypeFields)
                                     .First(p => p.Id == contentTypeId);

                var job = _db.BackgroundTasks.FirstOrDefault(p => p.Id == jobId);
                job.TaskStep = taskStep++;
                job.StatusInfo = $"Pulling records from file...";
                job.PercentComplete = 10;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                job.TaskStep = taskStep++;
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
                    bool validationSuccessful = ValidateContentItem(rowNumber, contentType, item, errorList, importAsDraft);
                    if (!validationSuccessful)
                    {
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
                            var fieldValues = GetFieldValuesFromRecord(contentType.ContentTypeFields, item, cancellationToken);
                            if (entity != null)
                            {
                                if (importMethod == ADD_NEW_RECORDS_ONLY)
                                {
                                    errorList.Add($"Row Number: {rowNumber}", $"Skipped importing.Please select {UPDATE_EXISTING_RECORDS_ONLY.Replace('_', ' ')} or {UPSERT_ALL_RECORDS.Replace('_', ' ')}.");
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
                            else if (importMethod == UPDATE_EXISTING_RECORDS_ONLY)
                            {
                                errorList.Add($"Row Number: {rowNumber}", $"{entityId} was not found for {contentType.LabelSingular}");
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
                            var fieldValues = await GetFieldValuesFromRecord(contentType.ContentTypeFields, item, cancellationToken);
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
                if (errorList != null && errorList.Count > 0)
                {
                    job.TaskStep = taskStep++;
                    job.StatusInfo = $"Generating CSV of failed imports...";
                    job.PercentComplete = 80;
                    _db.BackgroundTasks.Update(job);
                    await _db.SaveChangesAsync(cancellationToken);

                    var myExport = new Csv.CsvExport();
                    foreach (var error in errorList)
                    {
                        myExport.AddRow();
                        myExport[error.Key] = error.Value;
                    }
                    var csvExportAsBytes = myExport.ExportToBytes();
                    string fileName = $"{_currentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(DateTime.UtcNow)}-{contentType.DeveloperName}.csv";
                    var id = Guid.NewGuid();
                    var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(id.ToString(), fileName);
                    var mediaItem = new MediaItem
                    {
                        Id = id,
                        ObjectKey = objectKey,
                        ContentType = "text/csv",
                        FileName = fileName,
                        FileStorageProvider = _fileStorageProvider.GetName(),
                        Length = csvExportAsBytes.Length
                    };
                    _db.MediaItems.Add(mediaItem);

                    await _fileStorageProvider.SaveAndGetDownloadUrlAsync(csvExportAsBytes, objectKey, fileName, "text/csv", DateTime.UtcNow.AddYears(999));
                }

                job.TaskStep = taskStep++;
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

            private async Task<Dictionary<string, dynamic>> GetFieldValuesFromRecord(IEnumerable<ContentTypeField> contentTypeFields, Dictionary<string, object> record, CancellationToken cancellationToken)
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
                    else if (contentTypeField.FieldType.DeveloperName == BaseFieldType.Attachment)
                    {
                        string objectKey = await DownloadAndSaveFile(record[field].ToString(), contentTypeField.ContentType.DeveloperName, cancellationToken);
                        fieldValues.Add(field.ToDeveloperName(), objectKey);
                    }
                    else
                    {
                        fieldValues.Add(field.ToDeveloperName(), record[field]);
                    }
                }
                return fieldValues;
            }
            private bool ValidateContentItem(int rowNumber, ContentType contentType, Dictionary<string, dynamic> contentItem, Dictionary<string, string> errorList, bool importAsDraft)
            {
                var fields = contentItem.Keys.Where(s => s != BuiltInContentTypeField.Id.DeveloperName && s != BuiltInContentTypeField.Template.DeveloperName).ToList();

                foreach (var contentItemField in fields)
                {
                    var fieldDefinition = contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == contentItemField);
                    if (fieldDefinition == null)
                    {
                        errorList.Add($"Row Number: {rowNumber}", $"{contentItemField} is not a recognized field for {contentType.LabelSingular}.");
                        return false;
                    }
                    else
                    {
                        try
                        {
                            dynamic fieldValue;
                            if (fieldDefinition.FieldType.Label == BaseFieldType.MultipleSelect.Label)
                            {
                                fieldValue = fieldDefinition.FieldType.FieldValueFrom((contentItem[contentItemField] as string).Split(";"));
                            }
                            else if (fieldDefinition.FieldType.Label == BaseFieldType.Attachment)
                            {
                                bool isValidUrl = StringExtensions.IsValidUriFormat(contentItem[contentItemField]);
                                if (isValidUrl)
                                {
                                    fieldValue = fieldDefinition.FieldType.FieldValueFrom(contentItem[contentItemField]);
                                }
                                else
                                {
                                    errorList.Add($"Row Number: {rowNumber}", $"{fieldDefinition.Label} is invlaid.");
                                    return false;
                                }
                            }
                            else
                            {
                                fieldValue = fieldDefinition.FieldType.FieldValueFrom(contentItem[contentItemField]);
                            }
                            if (!importAsDraft && fieldDefinition.IsRequired && !fieldValue.HasValue)
                            {
                                errorList.Add($"Row Number: {rowNumber}", $"{fieldDefinition.Label} is a required field.");
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            errorList.Add($"Row Number: {rowNumber}", $"'{fieldDefinition.Label}' is an invalid format. {ex.Message}");
                            return false;
                        }
                    }
                }
                return true;
            }

            private async Task<string> DownloadAndSaveFile(string fileUrl, string contentTypeDeveloperName, CancellationToken cancellationToken)
            {
                try
                {
                    var fileInfo = await FileHelper.DownloadFile(fileUrl);
                    if (fileInfo != null)
                    {
                        var fileBytes = fileInfo.FileMemoryStream.ToArray();

                        string fileName = $"{_currentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(DateTime.UtcNow)}-{contentTypeDeveloperName}{fileInfo.FileExt}";
                        var id = ShortGuid.NewGuid();
                        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(id.ToString(), fileName);

                        await _fileStorageProvider.SaveAndGetDownloadUrlAsync(fileBytes, objectKey, fileName, fileInfo.ContentType, DateTime.UtcNow.AddYears(999));

                        var mediaItem = new MediaItem
                        {
                            Id = id,
                            ObjectKey = objectKey,
                            ContentType = fileInfo.ContentType,
                            FileName = fileName,
                            FileStorageProvider = _fileStorageProvider.GetName(),
                            Length = fileBytes.Length
                        };
                        _db.MediaItems.Add(mediaItem);
                        await _db.SaveChangesAsync(cancellationToken);

                        return objectKey;
                    }
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return string.Empty;
                }

            }
        }
    }
}
