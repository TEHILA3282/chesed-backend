using System.ComponentModel.DataAnnotations;

public class CreateDepositWithdrawDto
{
    [Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, MinLength(2)]
    public string RequestText { get; set; } = "";
}

public class DepositWithdrawResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string RequestText { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}
