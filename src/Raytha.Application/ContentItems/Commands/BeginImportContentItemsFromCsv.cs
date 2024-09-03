using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.MediaItems;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;
using System.Text.Json;

namespace Raytha.Application.ContentItems.Commands;

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
        public Validator(IRaythaDbContext db, ICsvService csvService)
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

                if (request.ImportMethod.IsNullOrEmpty())
                {
                    context.AddFailure("ImportMethod", "Import method is required.");
                    return;
                }

                ImportMethod importMethod;
                try
                {
                    importMethod = ImportMethod.From(request.ImportMethod);
                }
                catch (NotFoundException)
                {
                    context.AddFailure("ImportMethod", $"Unknown import method: {request.ImportMethod}");
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
                        var csvFile = csvService.ReadCsv<Dictionary<string, dynamic>>(stream);
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
                        if (importMethod == ImportMethod.UpdateExistingRecordsOnly && !csvFile.All(p => p.ContainsKey(BuiltInContentTypeField.Id.DeveloperName)))
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
        private readonly ICsvService _csvService;
        private readonly IFileStorageProvider _fileStorageProvider;
        private readonly ICurrentOrganization _currentOrganization;
        private readonly IFileStorageProviderSettings _fileStorageProviderSettings;

        private static HttpClient httpClient = new HttpClient();

        public BackgroundTask(
            IRaythaDbContext db,
            ICsvService csvService,
            IFileStorageProvider fileStorageProvider,
            IFileStorageProviderSettings fileStorageProviderSettings,
            ICurrentOrganization currentOrganization)
        {
            _db = db;
            _csvService = csvService;
            _fileStorageProvider = fileStorageProvider;
            _currentOrganization = currentOrganization;
            _fileStorageProviderSettings = fileStorageProviderSettings;
        }
        public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            Guid contentTypeId = args.GetProperty("ContentTypeId").GetProperty("Guid").GetGuid();
            ImportMethod importMethod = ImportMethod.From(args.GetProperty("ImportMethod").GetString());
            bool importAsDraft = args.GetProperty("ImportAsDraft").GetBoolean();
            Stream csvAsStream = new MemoryStream(args.GetProperty("CsvAsBytes").GetBytesFromBase64());

            ContentType contentType = _db.ContentTypes
                                    .Include(p => p.ContentTypeFields)
                                    .First(p => p.Id == contentTypeId);

            var records = _csvService.ReadCsv<Dictionary<string, dynamic>>(csvAsStream);

            int taskStep = 0; 
            var job = _db.BackgroundTasks.First(p => p.Id == jobId);
            job.TaskStep = taskStep++;
            job.StatusInfo = $"Pulled {records.Count()} from the CSV file. Beginning import.";
            job.PercentComplete = 10;
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            Dictionary<int, string> errorList = new Dictionary<int, string>();
            int rowNumber = 1;
            int successfullyImported = 0;

            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var contentItemIdsWebTemplateContentItemRelations = await _db.WebTemplateContentItemRelations
                .Include(wtr => wtr.WebTemplate)
                .Where(wtr => wtr.WebTemplate!.ThemeId == activeThemeId)
                .ToDictionaryAsync(wtr => wtr.ContentItemId, wtr => wtr, cancellationToken);

            var webTemplateDeveloperNamesIds = await _db.WebTemplates
                .Where(wt => wt.ThemeId == activeThemeId)
                .Select(wt => new { wt.Id, wt.DeveloperName })
                .ToDictionaryAsync(wt => wt.DeveloperName!, wt => wt.Id, cancellationToken);

            var contentItems = await _db.ContentItems
                .ToArrayAsync(cancellationToken);

            await foreach (var item in PrepareRecordsForImport(contentType, records, importAsDraft, cancellationToken))
            {
                if (!item.Success)
                {
                    errorList.Add(rowNumber, item.Error);
                    rowNumber++;
                    continue;
                }

                var currentRecord = contentItems.FirstOrDefault(ci => ci.Id == item.Result.ContentItem.Id);
                if (currentRecord == null && importMethod == ImportMethod.UpdateExistingRecordsOnly)
                {
                    rowNumber++;
                    continue;
                }

                if (currentRecord != null && importMethod == ImportMethod.AddNewRecordsOnly)
                {
                    rowNumber++;
                    continue;
                }

                var webTemplateId = webTemplateDeveloperNamesIds.TryGetValue(item.Result.WebTemplateDeveloperName, out var webTemplateIdByDeveloperName)
                    ? webTemplateIdByDeveloperName
                    : webTemplateDeveloperNamesIds[BuiltInWebTemplate.ContentItemDetailViewPage.DeveloperName];

                if (currentRecord != null)
                {
                    currentRecord.DraftContent = item.Result.ContentItem.DraftContent;
                    if (!importAsDraft)
                    {
                        currentRecord.PublishedContent = item.Result.ContentItem.PublishedContent;
                    }

                    currentRecord.IsDraft = item.Result.ContentItem.IsDraft;
                    currentRecord.IsPublished = item.Result.ContentItem.IsPublished;

                    _db.ContentItems.Update(currentRecord);

                    var webTemplateContentItemRelation = contentItemIdsWebTemplateContentItemRelations[currentRecord.Id];

                    webTemplateContentItemRelation.WebTemplateId = webTemplateId;

                    _db.WebTemplateContentItemRelations.Update(webTemplateContentItemRelation);
                }
                else
                {
                    _db.ContentItems.Add(item.Result.ContentItem);

                    var webTemplateContentItemRelation = new WebTemplateContentItemRelation
                    {
                        Id = Guid.NewGuid(),
                        ContentItemId = item.Result.ContentItem.Id,
                        WebTemplateId = webTemplateId,
                    };

                    await _db.WebTemplateContentItemRelations.AddAsync(webTemplateContentItemRelation, cancellationToken);
                }

                if (rowNumber % 10 == 0)
                {
                    job.TaskStep = taskStep;
                    job.StatusInfo = $"Processed {rowNumber} records of {records.Count()}";
                    int percentComplete = (int)((double)rowNumber / records.Count() * 100);
                    job.PercentComplete = percentComplete > 10 ? percentComplete : 10;
                    _db.BackgroundTasks.Update(job);
                }

                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    successfullyImported++;
                }
                catch (Exception ex)
                {
                    errorList.Add(rowNumber, ex.Message);
                }

                rowNumber++;
            }
            if (errorList.Any())
            {
                job.TaskStep = taskStep++;
                job.StatusInfo = $"Generating CSV of failed imports...";
                job.PercentComplete = 80;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                var myExport = new Csv.CsvExport();
                foreach (var key in errorList.Keys)
                {
                    myExport.AddRow();
                    myExport["Row Number"] = key.ToString();
                    myExport["Error Message"] = errorList[key];
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
                job.TaskStep = taskStep++;
                job.StatusInfo = JsonSerializer.Serialize(MediaItemDto.GetProjection(mediaItem));
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                job.TaskStep = taskStep++;
                job.StatusInfo = $"Finished importing.";
                job.PercentComplete = 100;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private async IAsyncEnumerable<CommandResponseDto<ContentItemDataFromCsv>> PrepareRecordsForImport(ContentType contentType, IEnumerable<Dictionary<string, dynamic>> records, bool importAsDraft, CancellationToken cancellationToken)
        {
            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var webTemplateDeveloperNames = await _db.WebTemplates
                .Where(wt => wt.ThemeId == activeThemeId)
                .Select(wt => wt.DeveloperName)
                .ToArrayAsync(cancellationToken);

            foreach (var record in records)
            {
                var templateDeveloperName = (record[BuiltInContentTypeField.Template.DeveloperName] as string)!.ToDeveloperName();

                if (!webTemplateDeveloperNames.Contains(templateDeveloperName))
                {
                    yield return new CommandResponseDto<ContentItemDataFromCsv>("Template", $"Template was not found with this developer name: {templateDeveloperName}");
                    continue;
                }

                var content = new Dictionary<string, dynamic>();

                foreach (var field in record)
                {
                    if (BuiltInContentTypeField.ReservedContentTypeFields.Any(p => p.DeveloperName == field.Key))
                        continue;

                    var fieldDefinition = contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == field.Key);
                    if (fieldDefinition != null)
                    {
                        string errorMessage = string.Empty;
                        BaseFieldValue fieldValue = null;
                        try
                        {

                            if (fieldDefinition.FieldType.DeveloperName == BaseFieldType.MultipleSelect.DeveloperName)
                            {
                                fieldValue = fieldDefinition.FieldType.FieldValueFrom(field.Value?.Split(";"));
                            }
                            else if (fieldDefinition.FieldType.DeveloperName == BaseFieldType.Attachment.DeveloperName)
                            {
                                try
                                {
                                    var mediaItem = await DownloadAndSaveFile(field.Value, cancellationToken);
                                    fieldValue = fieldDefinition.FieldType.FieldValueFrom(mediaItem.ObjectKey);
                                }
                                catch (Exception ex)
                                {
                                    errorMessage = ex.Message;
                                    fieldValue = fieldDefinition.FieldType.FieldValueFrom(string.Empty);
                                }
                            }
                            else
                            {
                                fieldValue = fieldDefinition.FieldType.FieldValueFrom(field.Value);
                            }

                            if (!importAsDraft && fieldDefinition.IsRequired && !fieldValue.HasValue)
                            {
                                errorMessage = $"Value is empty for required field: {fieldDefinition.DeveloperName}";
                            }
                        }
                        catch (Exception ex)
                        {
                            errorMessage = $"'{fieldDefinition.DeveloperName}' is in an invalid format.";
                        }
                        if (!errorMessage.IsNullOrEmpty())
                        {
                            yield return new CommandResponseDto<ContentItemDataFromCsv>("FieldError", errorMessage);
                            continue;
                        }
                        else
                        {
                            content.Add(field.Key, fieldValue.Value);
                        }
                    }
                }

                var contentItem = new ContentItem
                {
                    PublishedContent = content,
                    DraftContent = content,
                    IsPublished = importAsDraft == false,
                    IsDraft = importAsDraft,
                    ContentTypeId = contentType.Id
                };

                if (record.ContainsKey(BuiltInContentTypeField.Id.DeveloperName))
                {
                    ShortGuid idAsShortGuid = record[BuiltInContentTypeField.Id.DeveloperName];
                    if (idAsShortGuid == ShortGuid.Empty)
                    {
                        contentItem.Id = Guid.NewGuid();
                    }
                    else
                    {
                        contentItem.Id = idAsShortGuid;
                    }
                }
                else
                {
                    contentItem.Id = Guid.NewGuid();
                }

                var path = GetRoutePath(content, contentItem.Id, contentType);
                contentItem.Route = new Route
                {
                    Path = path,
                    ContentItemId = contentItem.Id
                };

                yield return new CommandResponseDto<ContentItemDataFromCsv>(new ContentItemDataFromCsv
                {
                    ContentItem = contentItem,
                    WebTemplateDeveloperName = templateDeveloperName,
                });
            }
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

        private async Task<MediaItem> DownloadAndSaveFile(string fileUrl, CancellationToken cancellationToken)
        {          
            if (!fileUrl.IsValidUriFormat())
                throw new Exception($"Invalid url format: {fileUrl}");

            var response = await httpClient.GetAsync(fileUrl);

            if (response == null)
                throw new Exception($"Unable to retrieve file from {fileUrl}. Reason unknown.");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Unable to retrieve file from {fileUrl}: {response.StatusCode} - {response.ReasonPhrase}");
            
            string fileName = Path.GetFileName(fileUrl);
            string contentType = response?.Content?.Headers?.ContentType?.ToString() ?? "Unknown";
            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);

            var fileBytes = memoryStream.ToArray();
            if (fileBytes.Length <= 0)
                throw new Exception($"File size is 0 bytes or corrupted.");

            if (fileBytes.Length > _fileStorageProviderSettings.MaxFileSize)
                throw new Exception($"File size of {fileUrl} is {fileBytes.Length}, which is greater than max file size of {_fileStorageProviderSettings.MaxFileSize}");

            var id = ShortGuid.NewGuid();
            var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(id.ToString(), fileName);

            await _fileStorageProvider.SaveAndGetDownloadUrlAsync(fileBytes, objectKey, fileName, contentType, DateTime.UtcNow.AddYears(999));

            var mediaItem = new MediaItem
            {
                Id = id,
                ObjectKey = objectKey,
                ContentType = contentType,
                FileName = fileName,
                FileStorageProvider = _fileStorageProvider.GetName(),
                Length = fileBytes.Length
            };
            _db.MediaItems.Add(mediaItem);
            await _db.SaveChangesAsync(cancellationToken);
            return mediaItem;
        }

        private record ContentItemDataFromCsv
        {
            public required ContentItem ContentItem { get; init; }
            public required string WebTemplateDeveloperName { get; init; }
        }
    }
}