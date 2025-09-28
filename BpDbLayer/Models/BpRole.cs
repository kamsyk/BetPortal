using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BpDbLayer.Models;

public partial class BpRole
{
    [Key]
    public int Id { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<BpUser> BpUsers { get; set; } = new List<BpUser>();
}
