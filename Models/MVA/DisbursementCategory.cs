using System.ComponentModel.DataAnnotations;

namespace MVAManagement.Models.MVA
{
    public class DisbursementCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        [MaxLength(10)]
        public string HexColor { get; set; } = "#94A3B8";   // used in dashboard chart

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;



        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
