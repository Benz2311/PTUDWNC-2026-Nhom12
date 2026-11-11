using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RecipeSlugHistory> RecipeSlugHistories => Set<RecipeSlugHistory>();
    public DbSet<RecipeAuditLog> RecipeAuditLogs => Set<RecipeAuditLog>();
    public DbSet<RecipeFavorite> RecipeFavorites => Set<RecipeFavorite>();
    public DbSet<RecipeRating> RecipeRatings => Set<RecipeRating>();
    public DbSet<RecipeComment> RecipeComments => Set<RecipeComment>();
    public DbSet<MealPlanEntry> MealPlanEntries => Set<MealPlanEntry>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<RecipeRevision> RecipeRevisions => Set<RecipeRevision>();
    public DbSet<ModerationLog> ModerationLogs => Set<ModerationLog>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanItem> MealPlanItems => Set<MealPlanItem>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<UserNutritionGoal> UserNutritionGoals => Set<UserNutritionGoal>();
    public DbSet<PersonalRecipeNote> PersonalRecipeNotes => Set<PersonalRecipeNote>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RecordActivities();
        UpdateConcurrencyTokens();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RecordActivities();
        UpdateConcurrencyTokens();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RecordActivities()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<Recipe>().ToList())
        {
            if (entry.State == EntityState.Added &&
                entry.Entity.Status is RecipeStatus.Published or RecipeStatus.Approved)
            {
                AddActivity(entry.Entity.AuthorId, ActivityType.CreatedRecipe, entry.Entity.Id);
                continue;
            }

            if (entry.State != EntityState.Modified ||
                entry.Entity.Status is not (RecipeStatus.Published or RecipeStatus.Approved))
                continue;

            var status = entry.Property(x => x.Status);
            var wasPublic = status.OriginalValue is RecipeStatus.Published or RecipeStatus.Approved;
            var contentChanged = entry.Property(x => x.Title).IsModified ||
                entry.Property(x => x.Description).IsModified ||
                entry.Property(x => x.Content).IsModified ||
                entry.Property(x => x.CategoryId).IsModified;
            if (status.IsModified && !wasPublic)
                AddActivity(entry.Entity.AuthorId, ActivityType.CreatedRecipe, entry.Entity.Id);
            else if (contentChanged)
                AddActivity(entry.Entity.AuthorId, ActivityType.UpdatedRecipe, entry.Entity.Id);
        }

        foreach (var entry in ChangeTracker.Entries<RecipeRating>().ToList())
        {
            if (entry.State == EntityState.Added ||
                entry.State == EntityState.Modified && entry.Property(x => x.Score).IsModified)
            {
                AddActivity(entry.Entity.UserId, ActivityType.RatedRecipe, entry.Entity.RecipeId);
            }
        }

        foreach (var entry in ChangeTracker.Entries<RecipeComment>().ToList())
        {
            var approved = entry.Entity.Status == ModerationStatus.Approved;
            var newlyApproved = entry.State == EntityState.Added && approved ||
                entry.State == EntityState.Modified &&
                entry.Property(x => x.Status).IsModified &&
                approved;
            if (newlyApproved)
                AddActivity(entry.Entity.UserId, ActivityType.CommentedRecipe, entry.Entity.Id);
        }
    }

    private void AddActivity(Guid userId, ActivityType activityType, Guid targetId)
    {
        var existsInTracker = ChangeTracker.Entries<ActivityLog>().Any(entry =>
            entry.State != EntityState.Deleted &&
            entry.Entity.UserId == userId &&
            entry.Entity.ActivityType == activityType &&
            entry.Entity.TargetId == targetId);
        if (!existsInTracker)
        {
            ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                ActivityType = activityType,
                TargetId = targetId
            });
        }
    }

    private void UpdateConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Category category:
                    category.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case Recipe recipe:
                    recipe.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case RecipeIngredient ingredient:
                    ingredient.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case RecipeStep step:
                    step.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case RecipeImage image:
                    image.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
                case RefreshToken refreshToken:
                    refreshToken.RowVersion = Guid.NewGuid().ToByteArray();
                    break;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (!isInMemory)
        {
            modelBuilder.HasPostgresExtension("unaccent");
            modelBuilder.HasPostgresExtension("pg_trgm");
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Recipe>(entity =>
        {
            if (!isInMemory)
            {
                entity.Property<NpgsqlTsVector>("SearchVectorFts")
                    .HasColumnName("SearchVector")
                    .HasColumnType("tsvector");
                entity.HasIndex("SearchVectorFts")
                    .HasMethod("GIN");
                    }
        });

        modelBuilder.Entity<RecipeSlugHistory>(entity =>
        {
            entity.ToTable("RecipeSlugHistories");
                    entity.HasKey(x => x.Id);
            entity.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(220);
            entity.HasIndex(x => new { x.RecipeId, x.Slug })
                .IsUnique();
            entity.HasIndex(x => x.Slug);
            entity.HasOne<Recipe>()
                .WithMany()
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeAuditLog>(entity =>
        {
            entity.ToTable("RecipeAuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(40);
            entity.HasIndex(x => new { x.RecipeId, x.CreatedAt });
        });
    }
}
