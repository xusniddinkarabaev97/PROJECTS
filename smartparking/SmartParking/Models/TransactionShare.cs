using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartParking.Models
{
    public class TransactionShare
    {
        public int Id { get; set; }

        public int TransactionId { get; set; }
        public Transaction Transaction { get; set; } = null!;

        public int SharePercentId { get; set; }
        public SharePercent SharePercent { get; set; } = null!;

        public int PlanId { get; set; }
        public Plan Plan { get; set; } = null!;

        public decimal Amount { get; set; }
    }
}
