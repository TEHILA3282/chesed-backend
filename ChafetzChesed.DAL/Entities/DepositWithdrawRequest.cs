using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChafetzChesed.DAL.Entities
{
    public class DepositWithdrawRequest
    {
        public int ID { get; set; }
        public string ClientID { get; set; } = "";   
        public int InstitutionId { get; set; }
        public decimal Amount { get; set; }
        public string RequestText { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}

