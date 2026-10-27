using System.Collections.Generic;
using CulinaryBlog.Application.DTOs;

namespace CulinaryBlog.Application.Common.Models;

/// <summary>
/// Chuẩn hóa metadata phân trang theo yêu cầu SRS:
/// {
///   "page": 1,
///   "pageSize": 10,
///   "total": 100,
///   "totalPages": 10,
///   "hasNextPage": true,
///   "hasPreviousPage": false
/// }
/// </summary>
public record PaginationMeta(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

/// <summary>
/// Chuẩn hóa Response Envelope theo yêu cầu SRS:
/// Success response: { "data": ..., "meta": ... }
/// - Không phân trang: { "data": ..., "meta": null }
/// - Phân trang: { "data": [ ... ], "meta": { "page": ..., "pageSize": ..., "total": ..., ... } }
/// </summary>
public record ApiResponse<T>(
    T Data,
    PaginationMeta? Meta = null)
{
    public static ApiResponse<T> Success(T data) => new(data, null);
    public static ApiResponse<T> Paged(T data, PaginationMeta meta) => new(data, meta);
}

/// <summary>
/// Factory helpers để tạo ApiResponse tiện lợi từ DTOs và PagedResult.
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data) => new(data, null);

    public static ApiResponse<IReadOnlyList<T>> Paged<T>(
        IReadOnlyList<T> data,
        PaginationMeta meta) => new(data, meta);

    public static ApiResponse<IReadOnlyList<T>> FromPaged<T>(PagedResultDto<T> paged) =>
        new(
            paged.Items,
            new PaginationMeta(
                paged.Page,
                paged.PageSize,
                paged.TotalCount,
                paged.TotalPages,
                paged.HasNextPage,
                paged.HasPreviousPage));

    public static ApiResponse<IReadOnlyList<T>> FromPaged<T>(PagedResult<T> paged) =>
        new(
            paged.Items,
            new PaginationMeta(
                paged.Page,
                paged.PageSize,
                paged.TotalCount,
                paged.TotalPages,
                paged.HasNextPage,
                paged.HasPreviousPage));
}
