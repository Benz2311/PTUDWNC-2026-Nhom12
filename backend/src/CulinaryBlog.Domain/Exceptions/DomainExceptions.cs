namespace CulinaryBlog.Domain.Exceptions;

/// <summary>
/// Lớp cơ sở trừu tượng cho tất cả các ngoại lệ nghiệp vụ thuộc Domain layer (Lab 3)
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Ngoại lệ khi dữ liệu hoặc quy tắc nghiệp vụ của RecipeStep không hợp lệ
/// </summary>
public class InvalidRecipeStepException : DomainException
{
    public InvalidRecipeStepException(string message) : base(message)
    {
    }
}

/// <summary>
/// Ngoại lệ khi dữ liệu hoặc quy tắc nghiệp vụ của RecipeImage không hợp lệ
/// </summary>
public class InvalidRecipeImageException : DomainException
{
    public InvalidRecipeImageException(string message) : base(message)
    {
    }
}

/// <summary>
/// Ngoại lệ chung cho các vi phạm quy tắc nghiệp vụ cốt lõi của Recipe
/// </summary>
public class RecipeDomainException : DomainException
{
    public RecipeDomainException(string message) : base(message)
    {
    }
}
