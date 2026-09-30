using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace api.Models;

public enum Gender
{
    Male,
    Female,
    Other
}

public enum UserStatus
{
    Active,
    Inactive
}

public enum UserRole
{
    Admin,
    User
}

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public Gender Gender { get; set; }
    public DateTime Dob { get; set; }
    public string? Address { get; set; }
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    [Required]
    [MinLength(8)]
    public required string password { get; set; }
    public bool isEmailVerified { get; set; } 
    public UserStatus Status { get; set; }
    public UserRole Role { get; set; }
}