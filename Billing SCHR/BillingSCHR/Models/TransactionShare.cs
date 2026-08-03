using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ODULink.Models
{
    /// <summary>
    /// Распределённая доля по конкретной транзакции
    /// </summary>
    public class TransactionShare
    {
        public int Id { get; set; }

        public int TransactionId { get; set; }
        public Transaction Transaction { get; set; }

        public int SharePercentId { get; set; }
        public SharePercent SharePercent { get; set; }

        public int PlanId { get; set; }
        public Plan Plan { get; set; }

        public decimal Amount { get; set; }
    }
}
