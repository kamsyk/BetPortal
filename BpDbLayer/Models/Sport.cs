using System.ComponentModel.DataAnnotations;
using BpDbLayer.Models.Interfaces;

namespace BpDbLayer.Models;

public partial class Sport : IModifiedFields
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    public bool Active { get; set; }

    [Required]
    public DateTime ModifiedDate { get; set; }

    [Required]
    public int ModifiedByUserId { get; set; }

    public virtual BpUser? ModifiedByUser { get; set; }
}
