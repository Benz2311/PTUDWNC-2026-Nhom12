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

        // Tiêu đề bước
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Nội dung hướng dẫn
        builder.Property(x => x.Description)
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

        // Một Recipe có nhiều RecipeStep
        builder.HasOne(x => x.Recipe)
            .WithMany(x => x.Steps)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Trong cùng một Recipe không được trùng số thứ tự bước
        builder.HasIndex(x => new
        {
            x.RecipeId,
            x.StepNumber
        })
        .IsUnique();

        // StepNumber phải bắt đầu từ 1
        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_RecipeSteps_StepNumber",
                "\"StepNumber\" >= 1"));
    }
}