using ChafetzChesed.DAL.Entities;
using System.Text.Json.Serialization;

public class LoanGuarantor
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int InstitutionId { get; set; }
    [JsonIgnore] public Loan Loan { get; set; } = null!;

    public string? IdNumber { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Occupation { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? LoanLink { get; set; }
    public string? Email { get; set; }
}
