using System.Text.Json.Serialization;

namespace ChafetzChesed.Common.DTOs
{
    public class CurrentUserDto
    {
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phoneNumber")] public string? PhoneNumber { get; set; }
        [JsonPropertyName("landlineNumber")] public string? LandlineNumber { get; set; }
        [JsonPropertyName("dateOfBirth")] public string? DateOfBirth { get; set; }
        [JsonPropertyName("personalStatus")] public string? PersonalStatus { get; set; }
        [JsonPropertyName("street")] public string? Street { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("houseNumber")] public string? HouseNumber { get; set; }
    }
}
