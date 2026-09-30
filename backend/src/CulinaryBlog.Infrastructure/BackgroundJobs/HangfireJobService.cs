using CulinaryBlog.Application.Interfaces;
using Hangfire;
using System.Linq.Expressions;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job service dùng Hangfire (Core + AspNetCore + PostgreSql) theo SRS.
/// </summary>
public sealed class HangfireJobService : IBackgroundJobService
{
    public string Enqueue(Expression<Action> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Enqueue(Expression<Func<Task>> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
        => BackgroundJob.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring(
        string jobId,
        Expression<Action> methodCall,
        string cronExpression)
        => RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
}
