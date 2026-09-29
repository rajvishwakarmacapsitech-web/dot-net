namespace api.Models;

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    public DateTime Dob { get; set; }

    public string Address { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}