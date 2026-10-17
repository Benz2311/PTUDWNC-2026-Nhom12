namespace CulinaryBlog.Application.Interfaces;

/// <summary>
/// Hợp đồng cho Background Job xử lý tạo ảnh Medium và Thumbnail (JOB-002 Image Resize / Thumbnail).
/// Chạy nền qua Hangfire worker.
/// </summary>
public interface IImageResizeJob
{
    /// <summary>
    /// Xử lý tải ảnh gốc, decode, resize tạo Medium và Thumbnail, tải lên MinIO và cập nhật Database.
    /// </summary>
    /// <param name="recipeImageId">Định danh bản ghi RecipeImage cần xử lý.</param>
    /// <param name="ct">Token hủy tác vụ.</param>
    Task ProcessAsync(Guid recipeImageId, CancellationToken ct = default);
}
