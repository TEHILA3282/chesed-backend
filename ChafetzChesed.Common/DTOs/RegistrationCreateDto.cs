// Common.DTOs
public class RegistrationCreateDto
{
    public string ID { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LandlineNumber { get; set; }
    public string? DateOfBirth { get; set; }

    public string? PersonalStatus { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? HouseNumber { get; set; }
}
