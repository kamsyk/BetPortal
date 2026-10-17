using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BpDbLayer.Models;

public partial class BpUserToken
{
    [Key]
    public int UserId { get; set; }

    [MaxLength(100)]
    required public string UserInitToken { get; set; }

    required public DateTime CreatedDate { get; set; }

    [ForeignKey(nameof(UserId))]
    required public virtual BpUser User { get; set; }
}
