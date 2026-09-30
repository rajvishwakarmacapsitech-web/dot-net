using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace api.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize(Roles = "Admin,User")]
public class PostsController : ControllerBase
{
    private readonly IMongoCollection<Post> _posts;
    private readonly CloudinaryService _cloudinary;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private bool IsAdmin => User.IsInRole(nameof(UserRole.Admin));

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
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

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
            UserId = userId,
            PostUrl = imageUrl
        };

        await _posts.InsertOneAsync(post);

        return Ok(post);
    }

    // GET ALL
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var filter = IsAdmin
            ? Builders<Post>.Filter.Empty
            : Builders<Post>.Filter.Eq(post => post.UserId, CurrentUserId);
        var posts = await _posts
            .Find(filter)
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
            .Find(BuildPostFilter(objectId))
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

        var filter = BuildPostFilter(objectId);

        var post = await _posts
            .Find(filter)
            .FirstOrDefaultAsync();

        if (post == null)
            return NotFound(new { message = "Post not found." });

        var update = Builders<Post>.Update
            .Set(x => x.Title, request.Title)
            .Set(x => x.Description, request.Content);

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

        var filter = BuildPostFilter(objectId);

        var result = await _posts.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
            return NotFound(new { message = "Post not found." });

        return Ok(new
        {
            message = "Post deleted successfully."
        });
    }

    private FilterDefinition<Post> BuildPostFilter(ObjectId objectId)
    {
        var filters = Builders<Post>.Filter;
        var postIdFilter = filters.Eq("_id", objectId);

        return IsAdmin
            ? postIdFilter
            : filters.And(postIdFilter, filters.Eq(post => post.UserId, CurrentUserId));
    }
}