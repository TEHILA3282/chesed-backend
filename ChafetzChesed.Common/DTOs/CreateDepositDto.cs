using System.Text.Json.Serialization;

namespace ChafetzChesed.Common.DTOs
{
    public class CreateDepositDto
    {
        [JsonPropertyName("depositTypeId")]
        public int DepositTypeId { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("purposeDetails")]
        public string? PurposeDetails { get; set; }

        [JsonPropertyName("isDirectDeposit")]
        public bool IsDirectDeposit { get; set; }

        [JsonPropertyName("depositDate")]
        public string? DepositDate { get; set; }

        [JsonPropertyName("depositReceivedDate")]
        public string? DepositReceivedDate { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string? PaymentMethod { get; set; }
    }
}
