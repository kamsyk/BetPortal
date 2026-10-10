using System.ComponentModel.DataAnnotations;
using BpDbLayer.Models.Interfaces;

namespace BpDbLayer.Models;

public partial class BpUserFavorites : ICreatedFields, IModifiedFields
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? CountryId { get; set; }

    public int? SportId { get; set; }

    public int? CompetitionId { get; set; }

    public int? CompetitorId { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    [Required]
    public DateTime ModifiedDate { get; set; }

    [Required]
    public int ModifiedByUserId { get; set; }

    public virtual BpUser? User { get; set; }

    public virtual Country? Country { get; set; }

    public virtual Sport? Sport { get; set; }

    public virtual Competition? Competition { get; set; }

    public virtual Competitor? Competitor { get; set; }

    public virtual BpUser? CreatedByUser { get; set; }

    public virtual BpUser? ModifiedByUser { get; set; }
}