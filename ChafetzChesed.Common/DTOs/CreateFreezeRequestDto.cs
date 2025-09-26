using ChafetzChesed.Common.Constants;

namespace ChafetzChesed.Common.DTOs
{
    public class CreateFreezeRequestDto
    {
        public string RequestType { get; set; } = FreezeRequestTypeValues.Loan;
        public string Reason { get; set; } = string.Empty;
        public bool Acknowledged { get; set; }
    }
}
