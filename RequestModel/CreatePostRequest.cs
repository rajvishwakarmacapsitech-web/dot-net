namespace api.Models;

public class CreatePostRequest
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public IFormFile? Image { get; set; }
}