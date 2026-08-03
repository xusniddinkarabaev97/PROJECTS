using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ODULink.Models
{
    /// <summary>
    /// Отделение госпиталя (хирургия, терапия, кардиология и т.д.)
    /// </summary>
    public class Department
    {
        [Key]
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Тип отделения: хирургическое, терапевтическое, диагностическое и т.д.</summary>
        public string? DepartmentType { get; set; }

        /// <summary>Этаж / корпус</summary>
        public string? Location { get; set; }

        /// <summary>Заведующий отделением</summary>
        public string? HeadDoctor { get; set; }

        /// <summary>Количество коек</summary>
        public int? BedCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Company? Company { get; set; }
    }
}
