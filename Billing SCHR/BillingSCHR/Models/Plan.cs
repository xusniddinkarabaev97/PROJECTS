using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ODULink.Models
{
    /// <summary>
    /// Тарифный план на медицинские услуги
    /// </summary>
    public class Plan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Категория услуг: консультация, операция, анализы, процедуры</summary>
        public string? Category { get; set; }

        /// <summary>Базовая стоимость</summary>
        public decimal? BasePrice { get; set; }

        public ICollection<SharePercent> SharePercents { get; set; }
    }
}
