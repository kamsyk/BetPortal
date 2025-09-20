using System.ComponentModel.DataAnnotations;

namespace BpDbLayer.Models
{
    internal class BpUser
    {
        public int Id { get; set; }

        [StringLength(40)]
        public string UserName { get; set; } = null!;

        public string? FirstName { get; set; }

        public string? Surname { get; set; }

        public string Email { get; set; } = null!;

        public string? Password { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }
    }
}
