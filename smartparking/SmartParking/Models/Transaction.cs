using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SmartParking.Enums;

namespace SmartParking.Models 
{
    public class Transaction
    {
        public int Id { get; set; }

        public int? StationId { get; set; }

        [Required]
        public int ClientId { get; set; }

        public Client Client { get; set; } = null!;

        [Required]
        public decimal TotalSum { get; set; }

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.New;

        public string? PaymentMethod { get; set; } // cash, card, click, payme, etc.

        public DateTime FilledAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string Status { get; set; } = "company"; // company, shop, parking...

        public int? CompanyId { get; set; }
    }
}