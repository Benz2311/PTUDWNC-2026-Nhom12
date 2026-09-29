using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable("RecipeSteps");

        // Khóa chính
        builder.HasKey(x => x.Id);

        // Khóa ngoại Recipe
        builder.Property(x => x.RecipeId)
            .IsRequired();

        // Thứ tự bước chế biến
        builder.Property(x => x.StepNumber)
            .IsRequired();

        // Tiêu đề bước (nullable theo SRS v1.2.0)
        builder.Property(x => x.Title)
            .HasMaxLength(200);

        // Nội dung hướng dẫn
        builder.Property(x => x.Description)
            .IsRequired();

        // Thời gian hẹn giờ (phút)
        builder.Property(x => x.TimerMinutes);

        // Đường dẫn hình ảnh minh họa bước thực hiện
        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

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

        // Một Recipe có nhiều RecipeStep
        builder.HasOne(x => x.Recipe)
            .WithMany(x => x.Steps)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Trong cùng một Recipe không được trùng số thứ tự bước (chỉ áp dụng cho record active theo SRS v1.2.0)
        builder.HasIndex(x => new
        {
            x.RecipeId,
            x.StepNumber
        })
        .IsUnique()
        .HasFilter("\"IsDeleted\" = false");

        // Ràng buộc StepNumber >= 1 và TimerMinutes >= 0 (hoặc NULL)
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_RecipeSteps_StepNumber",
                "\"StepNumber\" >= 1");

            t.HasCheckConstraint(
                "CK_RecipeSteps_TimerMinutes",
                "\"TimerMinutes\" IS NULL OR \"TimerMinutes\" >= 0");
        });
    }
}