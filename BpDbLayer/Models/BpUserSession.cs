using BpDbLayer.Models.Interfaces;

namespace BpDbLayer.Models;

public class BpUserSession : ICreatedFields
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime ExpiresDate { get; set; }

    public string IpAddress { get; set; } = null!;

    public string UserAgent { get; set; } = null!;

    public int CreatedByUserId { get; set; }

    public virtual BpUser User { get; set; } = null!;
}
