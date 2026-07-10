using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartParking.Models
{
    public class Plan
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int DurationHours { get; set; }

        [JsonIgnore]
        public ICollection<SharePercent> SharePercents { get; set; } = new List<SharePercent>();
    }
}
