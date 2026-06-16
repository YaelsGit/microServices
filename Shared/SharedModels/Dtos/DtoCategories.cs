namespace SharedModels.Dtos;

public class DtoCategories
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DtoCreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class DtoUpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
