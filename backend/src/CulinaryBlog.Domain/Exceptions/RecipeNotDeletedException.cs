using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class RecipeNotDeletedException()
    : BaseDomainException("Recipe is not soft deleted.", HttpStatusCode.Conflict, "RECIPE_NOT_DELETED");
