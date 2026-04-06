using System.ComponentModel.DataAnnotations;
using BpDbLayer.Models.Interfaces;

namespace BpDbLayer.Models;

public partial class BpRole : ICreatedFields
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string RoleName { get; set; } = null!;

    [StringLength(255)]
    public string Description { get; set; } = null!;

    [Required]
    public bool IsActive { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    [Required]
    public DateTime ModifiedDate { get; set; }

    [Required]
    public int ModifiedByUserId { get; set; }

    public virtual ICollection<BpUserBpRole> BpUser_BpRole { get; set; } = new List<BpUserBpRole>();
}
