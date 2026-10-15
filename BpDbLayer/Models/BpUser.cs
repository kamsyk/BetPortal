using System.ComponentModel.DataAnnotations;
using BpDbLayer.Models.Interfaces;

namespace BpDbLayer.Models;

public partial class BpUser : ICreatedFields
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string UserName { get; set; } = null!;

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? Surname { get; set; }

    [Required]
    [StringLength(200)]
    public string Email { get; set; } = null!;

    public string? Password { get; set; }

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

    public UserToken? UserToken { get; set; }

    public virtual ICollection<BpUserBpRole> BpUser_BpRole { get; set; } = new List<BpUserBpRole>();

    public ICollection<BpUserLoginProvider> UserLoginProviders { get; set; } = new List<BpUserLoginProvider>();
}
