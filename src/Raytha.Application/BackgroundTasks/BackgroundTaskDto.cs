using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using System.Linq.Expressions;

namespace Raytha.Application.BackgroundTasks;

public record BackgroundTaskDto : BaseEntityDto
{
    public string Name { get; set; }
    public string? Args { get; set; }
    public string? ErrorMessage { get; set; }
    public BackgroundTaskStatus Status { get; set; }
    public string? StatusInfo { get; set; }
    public int PercentComplete { get; set; }
    public int NumberOfRetries { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public int TaskStep { get; set; }

    public static Expression<Func<BackgroundTask, BackgroundTaskDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }
    public static BackgroundTaskDto GetProjection(BackgroundTask entity)
    {
        return new BackgroundTaskDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            CompletionTime = entity.CompletionTime,
            Args = entity.Args,
            ErrorMessage = entity.ErrorMessage,
            Status = entity.Status,
            StatusInfo = entity.StatusInfo,
            PercentComplete = entity.PercentComplete,
            NumberOfRetries = entity.NumberOfRetries,
            TaskStep = entity.TaskStep
        };
    }
}
