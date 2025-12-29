using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BpDbLayer.Models;

public partial class BpRole
{
    public int Id { get; set; }

    public string RoleName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<BpUser_BpRole> BpUser_BpRole { get; set; } = new List<BpUser_BpRole>();
}
