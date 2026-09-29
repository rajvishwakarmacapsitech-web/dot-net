using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize(Roles = "Admin,User")]
public class PostsController : ControllerBase
{
    private readonly IMongoCollection<Post> _posts;
    private readonly CloudinaryService _cloudinary;

    public PostsController(
        IMongoDatabase database,
        CloudinaryService cloudinary)
    {
        _posts = database.GetCollection<Post>("posts");
        _cloudinary = cloudinary;
    }

    // CREATE
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

        await _posts.InsertOneAsync(post);

        return Ok(post);
    }

    // GET ALL
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var posts = await _posts
            .Find(_ => true)
            .ToListAsync();

        return Ok(posts);
    }

    // GET BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest(new { message = "Invalid post id." });

        var post = await _posts
            .Find(Builders<Post>.Filter.Eq("_id", objectId))
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound(new { message = "Post not found." });

        return Ok(post);
    }

    // UPDATE
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        string id,
        [FromForm] CreatePostRequest request)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest(new { message = "Invalid post id." });

        var filter = Builders<Post>.Filter.Eq("_id", objectId);

        var post = await _posts
            .Find(filter)
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound(new { message = "Post not found." });

        var update = Builders<Post>.Update
            .Set(x => x.Title, request.Title)
            .Set(x => x.Description, request.Content)
            .Set(x => x.UserId, request.UserId ?? string.Empty);

        if (request.Image != null)
        {
            var imageUrl = await _cloudinary.UploadAsync(request.Image);

            if (!string.IsNullOrEmpty(imageUrl))
            {
                update = update.Set(x => x.PostUrl, imageUrl);
            }
        }

        await _posts.UpdateOneAsync(filter, update);

        var updatedPost = await _posts
            .Find(filter)
            .FirstOrDefaultAsync();

        return Ok(updatedPost);
    }

    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest(new { message = "Invalid post id." });

        var filter = Builders<Post>.Filter.Eq("_id", objectId);

        var result = await _posts.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
            return NotFound(new { message = "Post not found." });

        return Ok(new
        {
            message = "Post deleted successfully."
        });
    }
}