using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace BpDbLayer.Models
{
    public class BpUserBpRole
    {
        public int UserId { get; set; }

        public int RoleId { get; set; }

        [ForeignKey(nameof(UserId))]
        public BpUser User { get; set; } = null!;

        [ForeignKey(nameof(RoleId))]
        public BpRole Role { get; set; } = null!;
    }
}
