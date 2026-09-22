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

        // Đường dẫn hình ảnh
        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(2048);

        // Nội dung mô tả hình ảnh
        builder.Property(x => x.AltText)
            .HasMaxLength(500);

        // Ảnh đại diện của công thức
        builder.Property(x => x.IsPrimary)
            .IsRequired();

        // Thứ tự hiển thị hình ảnh
        builder.Property(x => x.SortOrder)
            .IsRequired();

        // Thông tin quản lý dữ liệu
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        // Kiểm soát cập nhật đồng thời
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Một Recipe có nhiều RecipeImage
        builder.HasOne(x => x.Recipe)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Hỗ trợ sắp xếp ảnh theo từng Recipe
        builder.HasIndex(x => new
        {
            x.RecipeId,
            x.SortOrder
        });

        // SortOrder không được âm
        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_RecipeImages_SortOrder",
                "\"SortOrder\" >= 0"));
    }
}