using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class RecipeImageConfiguration : IEntityTypeConfiguration<RecipeImage>
{
    public void Configure(EntityTypeBuilder<RecipeImage> builder)
    {
        builder.ToTable("RecipeImages");

        // Khóa chính
        builder.HasKey(x => x.Id);

        // Khóa ngoại Recipe
        builder.Property(x => x.RecipeId)
            .IsRequired();

        // Đường dẫn hình ảnh gốc (bắt buộc)
        builder.Property(x => x.OriginalUrl)
            .IsRequired()
            .HasMaxLength(2048);

        // Đường dẫn hình ảnh cỡ trung (800x600)
        builder.Property(x => x.MediumUrl)
            .HasMaxLength(2048);

        // Đường dẫn hình ảnh thu nhỏ (300x300)
        builder.Property(x => x.ThumbnailUrl)
            .HasMaxLength(2048);

        // Nội dung mô tả hình ảnh (tối đa 200 ký tự theo SRS v1.2.0)
        builder.Property(x => x.AltText)
            .HasMaxLength(200);

        // Ảnh đại diện của công thức
        builder.Property(x => x.IsPrimary)
            .IsRequired();

        // Thứ tự hiển thị hình ảnh (OrderIndex theo SRS v1.2.0)
        builder.Property(x => x.OrderIndex)
            .IsRequired();

        // Thông tin quản lý dữ liệu
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        // PostgreSQL không tự sinh rowversion như SQL Server.
        // Vì vậy RowVersion sẽ được ứng dụng gán giá trị.
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        // Một Recipe có nhiều RecipeImage
        builder.HasOne(x => x.Recipe)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Hỗ trợ sắp xếp ảnh theo từng Recipe
        builder.HasIndex(x => new
        {
            x.RecipeId,
            x.OrderIndex
        });

        // Đảm bảo tối đa 1 active primary image trên mỗi Recipe theo SRS v1.2.0
        builder.HasIndex(x => new
        {
            x.RecipeId,
            x.IsPrimary
        })
        .IsUnique()
        .HasFilter("\"IsPrimary\" = true AND \"IsDeleted\" = false");

        // OrderIndex không được âm
        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_RecipeImages_OrderIndex",
                "\"OrderIndex\" >= 0"));
    }
}