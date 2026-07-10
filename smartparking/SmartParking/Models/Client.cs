using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartParking.Models 
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ExternalId { get; set; } = null!; // Payme account.user_id yoki order_id

        [MaxLength(150)]
        public string? FullName { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsVerified { get; set; } = false;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Source { get; set; } = "payme"; // payme, click, manual

        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, blocked, deleted

        public List<Transaction> Transactions { get; set; } = new();
    }
}