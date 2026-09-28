using api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
namespace api.Controllers;

[Route("api")]
public class UsersController : ControllerBase
{
    private readonly IMongoCollection<User> _users;

    public UsersController(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
      
        await _users.InsertOneAsync(user);
        //Console.WriteLine(JsonSerializer.Serialize(user));

        return Ok(user);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _users.Find(_ => true).ToListAsync();

        return Ok(users);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var objectId = new ObjectId(id);

        var user = await _users
            .Find(Builders<User>.Filter.Eq("_id", objectId))
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] User user)
    {
        var objectId = new ObjectId(id);

        var filter = Builders<User>.Filter.Eq("_id", objectId);

        var update = Builders<User>.Update
            .Set(u => u.Name, user.Name)
            .Set(u => u.Phone, user.Phone)
            .Set(u => u.Gender, user.Gender)
            .Set(u => u.Dob, user.Dob)
            .Set(u => u.Address, user.Address);

        var result = await _users.UpdateOneAsync(filter, update);

        if (result.MatchedCount == 0)
            return NotFound();

        return Ok(user);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var objectId = new ObjectId(id);

        var filter = Builders<User>.Filter.Eq("_id", objectId);

        var result = await _users.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
            return NotFound();

        return Ok(new
        {
            message = "User deleted successfully"
        });
    }
}