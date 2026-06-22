using System.ComponentModel.DataAnnotations;
using BpDbLayer.Models.Interfaces;

namespace BpDbLayer.Models;

public partial class Competition : IModifiedFields
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    public int SportId { get; set; }

    [Required]
    public int CountryId { get; set; }

    [Required]
    public DateTime ModifiedDate { get; set; }

    [Required]
    public int ModifiedByUserId { get; set; }

    public virtual Sport? Sport { get; set; }

    public virtual Country? Country { get; set; }

    public virtual BpUser? ModifiedByUser { get; set; }
}
