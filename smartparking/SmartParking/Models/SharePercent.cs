using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartParking.Models
{
    public class SharePercent
    {
        public int Id { get; set; }
        public string ShareholderName { get; set; } = string.Empty;
        public decimal Percent { get; set; }

        public int PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
    }
}
