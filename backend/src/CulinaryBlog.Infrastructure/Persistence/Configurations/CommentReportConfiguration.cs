using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class CommentReportConfiguration : IEntityTypeConfiguration<CommentReport>
{
    public void Configure(EntityTypeBuilder<CommentReport> builder)
    {
        builder.ToTable("CommentReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Details)
            .HasMaxLength(1000)
            .HasColumnType("text");

        builder.Property(r => r.Status)
            .IsRequired();

        builder.Property(r => r.ResolutionNotes)
            .HasMaxLength(1000)
            .HasColumnType("text");

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // Index phục vụ kiểm tra chống trùng lặp báo cáo và truy vấn theo trạng thái
        builder.HasIndex(r => new { r.CommentId, r.ReporterId, r.Status });
        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        builder.HasOne(r => r.Comment)
            .WithMany(c => c.Reports)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reporter)
            .WithMany(u => u.ReportsSubmitted)
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ResolvedByUser)
            .WithMany()
            .HasForeignKey(r => r.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
