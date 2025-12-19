using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BpDbLayer.Models;

public partial class BpUser
{
    [Key]
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? Surname { get; set; }

    public string Email { get; set; } = null!;

    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<BpRole> BpRoles { get; set; } = new List<BpRole>();
}
