# Implement background task in .NET using Raytha infrastructure

Raytha offers an infrastructure for implementing long-running tasks in .NET 6+ applications. With the introduction of Hosted Services and the BackgroundService base class in .NET 6+, developers can now create tasks that operate independently of the web thread. However, effectively implementing this functionality requires careful consideration.

There are numerous scenarios where running a background task becomes necessary, and one prominent example employed by Raytha is the "Export Data to CSV" use case. When dealing with extensive data sets that exceed the limits of a single web request, executing the task in the background becomes essential. Subsequently, you can monitor the task's progress by periodically checking its status.

## Alternatives to Raytha's built in background task manager

There are robust and battle tested background task managers in the .NET ecosystem such as Hangfire and Quartz.NET. If you need a full featured background task manager, we recommend you go with one of those alternatives. However, neither of those alternatives are MIT open source licensed.

## Implement a background task

### Create a class that implements the IExecuteBackgroundTask interface.

The Execute() method requires the jobId and JsonElement args as input parameters. By utilizing the args.GetProperty() method, you can easily extract the values from the payload. The jobId serves as a unique identifier to retrieve the corresponding background task, allowing you to perform periodic updates on it.

Within your Execute() method, you will implement the business logic for your long-running task. Here's a simplified example: retrieve the background task and continuously update it with the latest status until the PercentComplete reaches 100.

To provide additional information to the consumer without displaying new messaging to the end user just yet, you can make use of the TaskStep attribute. This attribute serves as a signal to update the UI, indicating that there is more information available, such as an updated PercentComplete.

    public class BackgroundTask : IExecuteBackgroundTask
    {
        private readonly IRaythaDbContext _entityFrameworkDb;

        public BackgroundTask(
            IRaythaDbContext entityFrameworkDb)
        {
            _entityFrameworkDb = entityFrameworkDb;
        }

        public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            IEnumerable<ContentItemDto> items;

            Guid contentItemId = args.GetProperty("ContentItemId").GetProperty("Guid").GetGuid();

            var job = _entityFrameworkDb.BackgroundTasks.First(p => p.Id == jobId);
            job.TaskStep = 1;
            job.StatusInfo = $"Pulling content item...";
            job.PercentComplete = 0;
            _entityFrameworkDb.BackgroundTasks.Update(job);
            await _entityFrameworkDb.SaveChangesAsync(cancellationToken);

            var contentItem = _entityFrameworkDb.ContentItems.First(p => p.Id == contentItemId);
            job.TaskStep = 2;
            job.StatusInfo = $"Item pulled.";
            _entityFrameworkDb.BackgroundTasks.Update(job);

            //do something that takes a while...

            job.TaskStep = 3;
            job.PercentComplete = 100;
            job.StatusInfo = JsonSerializer.Serialize(ContentItemDto.GetProjection(contentItem));
            _entityFrameworkDb.BackgroundTasks.Update(job);
            await _entityFrameworkDb.SaveChangesAsync(cancellationToken);
        }
    }

### Add your class to the dependency injection

You must add this class into the dependency injection. This is typically done in the Raytha.Application's ConfigureServices.cs.

    services.AddScoped<BeginExportContentItemsToCsv.BackgroundTask>();

### Enqueue the background task

Once you have an object capable of executing the business logic for the background task, the next step is to enqueue it for processing.

To achieve this, you should inject the `IBackgroundTaskQueue` class into your constructor.

To enqueue the background task for execution, you can use the following command:

    var backgroundJobId = await _taskQueue.EnqueueAsync<BackgroundTask>(request, cancellationToken);

In the above code snippet, `BackgroundTask` represents the class you defined and constructed in the previous step. `request` is an object of your choice that will be serialized as JSON and used as the data payload, acting as the args in your BackgroundTask class.

As a result of enqueuing the task, you will receive a `backgroundJobId`. You can provide this identifier back to the consumer, enabling them to periodically poll the background tasks to obtain status updates.

### Poll for status updates.

You can run the Query for `GetBackgroundTaskById.Query` [(see docs)](/api/Raytha.Application.BackgroundTasks.Queries.GetBackgroundTaskById.Query.html) and pass in the Job Id from the step above to retrieve the job id at that time. You can use the `StatusInfo`, `TaskStep`, and `PercentComplete` attributes to update the UI or other systems as needed.
