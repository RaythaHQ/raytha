using MediatR;
using Raytha.Application.Common.Models;
using CSharpVitamins;
using Raytha.Application.Common.Interfaces;
using FluentValidation;
using Raytha.Application.Common.Exceptions;
using Raytha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Raytha.Domain.ValueObjects;
using Raytha.Application.Common.Utils;
using Csv;
using Raytha.Domain.Common;
using Raytha.Domain.ValueObjects.FieldValues;
using System.Text.Json;
using Raytha.Application.MediaItems;

namespace Raytha.Application.ContentItems.Commands;

public class BeginExportContentItemsToCsv
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid ViewId { get; init; }
        public bool ExportOnlyColumnsFromView { get; init; } = true;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ViewId).NotEmpty();
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
        private readonly IContentTypeInRoutePath _contentTypeInRoutePath;
        private readonly ICurrentOrganization _currentOrganization;
        private readonly IFileStorageProvider _fileStorageProvider;
        private readonly FieldValueConverter _fieldValueConverter;

        public BackgroundTask(
            ICurrentOrganization currentOrganization, 
            IRaythaDbJsonQueryEngine db, 
            IRaythaDbContext entityFrameworkDb, 
            IFileStorageProvider fileStorageProvider,
            FieldValueConverter fieldValueConverter)
        {
            _db = db;
            _entityFrameworkDb = entityFrameworkDb;
            _currentOrganization = currentOrganization;
            _fileStorageProvider = fileStorageProvider;
            _fieldValueConverter = fieldValueConverter;
        }
        public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            IEnumerable<ContentItemDto> items;

            Guid viewId = args.GetProperty("ViewId").GetProperty("Guid").GetGuid();
            bool exportOnlyColumnsFromView = args.GetProperty("ExportOnlyColumnsFromView").GetBoolean();

            View view = _entityFrameworkDb.Views
                .Include(p => p.ContentType)
                .ThenInclude(p => p.ContentTypeFields)
                .FirstOrDefault(p => p.Id == viewId);

            if (view == null)
                throw new NotFoundException("View", viewId);

            string[] filters = GetFiltersForView(view);
            string finalOrderBy = GetSortForView(view);

            int count = _db.CountContentItems(view.ContentTypeId, new string[0], string.Empty, filters);

            var job = _entityFrameworkDb.BackgroundTasks.First(p => p.Id == jobId);
            job.TaskStep = 1;
            job.StatusInfo = $"Pulling {count} items for {view.ContentType.LabelPlural} - {view.Label}";
            job.PercentComplete = 0;
            _entityFrameworkDb.BackgroundTasks.Update(job);
            await _entityFrameworkDb.SaveChangesAsync(cancellationToken);

            var myExport = new CsvExport();
            int currentIndex = 0;
            var activeThemeId = await _entityFrameworkDb.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var contentItemIdsTemplateLabels = await _entityFrameworkDb.WebTemplateContentItemRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == activeThemeId)
                .Select(wtr => new { wtr.ContentItemId, wtr.WebTemplate!.Label })
                .ToDictionaryAsync(wtr => wtr.ContentItemId, wtr => wtr.Label, cancellationToken);

            foreach (var item in _db.QueryAllContentItemsAsTransaction(view.ContentTypeId,
                                                new string[0],
                                                string.Empty,
                                                filters,
                                                finalOrderBy))
            {
                var webTemplateLabel = contentItemIdsTemplateLabels[item.Id];
                var contentItemAsDict = _fieldValueConverter.MapToListItemValues(ContentItemDto.GetProjection(item), webTemplateLabel);

                myExport.AddRow();
                if (exportOnlyColumnsFromView)
                {
                    foreach (var column in view.Columns)
                    {
                        if (contentItemAsDict.ContainsKey(column))
                        {
                            myExport[column] = contentItemAsDict[column];
                        }
                        else
                        {
                            myExport[column] = string.Empty;
                        }
                    }
                }
                else
                {
                    foreach (var column in BuiltInContentTypeField.ReservedContentTypeFields)
                    {
                        if (contentItemAsDict.ContainsKey(column))
                        {
                            myExport[column] = contentItemAsDict[column];
                        }
                        else
                        {
                            myExport[column] = string.Empty;
                        }
                    }
                    foreach (var column in view.ContentType.ContentTypeFields.Select(p => p.DeveloperName))
                    {
                        if (contentItemAsDict.ContainsKey(column))
                        {
                            myExport[column] = contentItemAsDict[column];
                        }
                        else
                        {
                            myExport[column] = string.Empty;
                        }
                    }
                }

                currentIndex++;
                int percentDone = (currentIndex / count) * 100;
                if (percentDone % 20 == 0) 
                {
                    job.TaskStep = 2;
                    job.PercentComplete = percentDone - 10;
                    _entityFrameworkDb.BackgroundTasks.Update(job);
                    await _entityFrameworkDb.SaveChangesAsync(cancellationToken);
                }
            }

            job.TaskStep = 3;
            job.StatusInfo = $"Items pulled. Generating csv...";
            _entityFrameworkDb.BackgroundTasks.Update(job);
            await _entityFrameworkDb.SaveChangesAsync(cancellationToken);

            var csvAsBytes = myExport.ExportToBytes();
            string fileName = $"{_currentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(DateTime.UtcNow)}-{view.DeveloperName}.csv";
            var id = Guid.NewGuid();
            var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(id.ToString(), fileName);
            var mediaItem = new MediaItem
            {
                Id = id,
                ObjectKey = objectKey,
                ContentType = "text/csv",
                FileName = fileName,
                FileStorageProvider = _fileStorageProvider.GetName(),
                Length = csvAsBytes.Length
            };
            _entityFrameworkDb.MediaItems.Add(mediaItem);
          
            await _fileStorageProvider.SaveAndGetDownloadUrlAsync(csvAsBytes, objectKey, fileName, "text/csv", DateTime.UtcNow.AddYears(999));

            job.TaskStep = 4;
            job.PercentComplete = 100;
            job.StatusInfo = JsonSerializer.Serialize(MediaItemDto.GetProjection(mediaItem));
            _entityFrameworkDb.BackgroundTasks.Update(job);
            await _entityFrameworkDb.SaveChangesAsync(cancellationToken);
        }

        protected string GetSortForView(View view)
        {
            string finalOrderBy = $"{BuiltInContentTypeField.CreationTime.DeveloperName} {SortOrder.DESCENDING}";
            var viewOrderBy = view.Sort.Select(p => $"{p.DeveloperName} {p.SortOrder.DeveloperName}").ToList();
            finalOrderBy = viewOrderBy.Any() ? string.Join(",", viewOrderBy) : finalOrderBy;

            return finalOrderBy;
        }

        protected string[] GetFiltersForView(View view)
        {
            var conditionToODataUtility = new FilterConditionToODataUtility(view.ContentType);
            var oDataFromFilter = conditionToODataUtility.ToODataFilter(view.Filter);
            return new string[] { oDataFromFilter };
        }
    }
}
