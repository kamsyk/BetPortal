using System.ComponentModel.DataAnnotations;

namespace BpDbLayer.Models
{
    public class BpUser
    {
        public int Id { get; set; }

        [StringLength(40)]
        public string UserName { get; set; } = null!;

        [StringLength(40)]
        public string? FirstName { get; set; }

        [StringLength(40)]
        public string? Surname { get; set; }

        [StringLength(80)]
        public string Email { get; set; } = null!;

        [StringLength(40)]
        public string? Password { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }
    }
}
