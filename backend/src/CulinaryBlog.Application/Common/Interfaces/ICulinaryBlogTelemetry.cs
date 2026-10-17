namespace CulinaryBlog.Application.Common.Interfaces;

/// <summary>
/// Abstraction cho Telemetry & Business Metrics trong hệ thống:
/// - RecordRecipeCreated: ghi nhận metric khi tạo công thức thành công
/// - RecordRecipePublished: ghi nhận metric khi xuất bản công thức thành công
/// - RecordHttpRequest: ghi nhận tổng request, thời gian xử lý và lỗi HTTP
/// - RecordError: ghi nhận lỗi hệ thống
/// </summary>
public interface ICulinaryBlogTelemetry
{
    void RecordRecipeCreated(string? category = null);

    void RecordRecipePublished(string? category = null);

    void RecordHttpRequest(string method, string path, int statusCode, double durationMs);

    void RecordError(string errorType, string? endpoint = null);
}
