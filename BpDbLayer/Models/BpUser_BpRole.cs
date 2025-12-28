using System.Data;

namespace BpDbLayer.Models
{
    public class BpUser_BpRole
    {
        public int UserId { get; set; }

        public int RoleId { get; set; }

        // Navigation properties
        public BpUser User { get; set; } = null!;

        public BpRole Role { get; set; } = null!;
    }
}
