using api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using api.Services;
using System.Text.Json;
namespace api.Controllers;


[ApiController]
[Route("api/posts")]
public class PostsController : ControllerBase
{
    private readonly IMongoCollection<Post> _posts; 
    private readonly CloudinaryService _cloudinary;

    public PostsController(IMongoDatabase database, CloudinaryService cloudinary )
    {
        _posts = database.GetCollection<Post>("posts");
        _cloudinary = cloudinary;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
     [FromForm] CreatePostRequest request)
    {
        string imageUrl = string.Empty;

        if (request.Image != null)
        {
            imageUrl = await _cloudinary.UploadAsync(request.Image)
                ?? string.Empty;
        }

        var post = new Post
        {
            Title = request.Title,
            Description = request.Content,
            UserId = request.UserId ?? string.Empty,
            PostUrl = imageUrl
        };

        Console.WriteLine(post);
        //Console.WriteLine(post);

        await _posts.InsertOneAsync(post);

        return Ok(post);
    }
}