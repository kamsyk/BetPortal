using System.ComponentModel.DataAnnotations;

namespace BpDbLayer.Models;

public class BpUserLoginProvider
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int ProviderId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = null!;

    [Required]
    public BpUser User { get; set; } = null!;

    [Required]
    public LoginProvider LoginProvider { get; set; } = null!;
}
