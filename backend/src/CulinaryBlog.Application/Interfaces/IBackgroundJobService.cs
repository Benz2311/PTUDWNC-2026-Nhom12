namespace CulinaryBlog.Application.Interfaces;

/// <summary>Background job service interface — implemented by HangfireJobService in Infrastructure.</summary>
public interface IBackgroundJobService
{
    /// <summary>Enqueue một job chạy ngay lập tức.</summary>
    string Enqueue(System.Linq.Expressions.Expression<Action> methodCall);

    /// <summary>Enqueue một job async.</summary>
    string Enqueue(System.Linq.Expressions.Expression<Func<Task>> methodCall);

    /// <summary>Lên lịch job chạy sau một khoảng thời gian.</summary>
    string Schedule(System.Linq.Expressions.Expression<Action> methodCall, TimeSpan delay);

    /// <summary>Đăng ký recurring job theo cron expression.</summary>
    void AddOrUpdateRecurring(
        string jobId,
        System.Linq.Expressions.Expression<Action> methodCall,
        string cronExpression);
}
