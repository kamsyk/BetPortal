using System.ComponentModel.DataAnnotations;
using BpDbLayer.Models.Interfaces;

public partial class BpUserFavorite : ICreatedFields, IModifiedFields
{
    [Required]
    public int UserId { get; set; }

    public int CountryId { get; set; }

    public int SportId { get; set; }

    public int CompetitionId { get; set; }

    public int CompetitorId { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    [Required]
    public DateTime ModifiedDate { get; set; }

    [Required]
    public int ModifiedByUserId { get; set; }
}