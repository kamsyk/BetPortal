using System.ComponentModel.DataAnnotations;

namespace BpDbLayer.Models;

public class LoginProvider
{
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ProviderName { get; set; } = null!;

    public ICollection<BpUserLoginProvider> UserLoginProviders { get; set; } = new List<BpUserLoginProvider>();
}
