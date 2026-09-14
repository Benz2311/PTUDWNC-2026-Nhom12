using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration: IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasData(
            new
            {
                Id = CategoryIds[0],
                Name = "Món Chính",
                Slug = "mon-chinh",
                Description = "Các món ăn chính trong bữa cơm Việt",
                OrderIndex = 1,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new
            {
                Id = CategoryIds[1],
                Name = "Món Canh",
                Slug = "mon-canh",
                Description = "Canh và súp truyền thống",
                OrderIndex = 2,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new
            {
                Id = CategoryIds[2],
                Name = "Món Tráng Miệng",
                Slug = "mon-trang-mieng",
                Description = "Các loại bánh, chè và tráng miệng",
                OrderIndex = 3,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new
            {
                Id = CategoryIds[3],
                Name = "Đồ Uống",
                Slug = "do-uong",
                Description = "Nước ép, sinh tố và thức uống",
                OrderIndex = 4,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new
            {
                Id = CategoryIds[4],
                Name = "Ăn Vặt",
                Slug = "an-vat",
                Description = "Snack và đồ ăn nhẹ",
                OrderIndex = 5,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            }
        );
    }

    private static readonly Guid[] CategoryIds =
[
    Guid.Parse("10000000-0000-0000-0000-000000000001"),
    Guid.Parse("10000000-0000-0000-0000-000000000002"),
    Guid.Parse("10000000-0000-0000-0000-000000000003"),
    Guid.Parse("10000000-0000-0000-0000-000000000004"),
    Guid.Parse("10000000-0000-0000-0000-000000000005")
];
}