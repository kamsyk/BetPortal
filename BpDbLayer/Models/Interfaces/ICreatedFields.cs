using System.ComponentModel.DataAnnotations;

namespace BpDbLayer.Models.Interfaces
{
    public interface ICreatedFields
    {
        public DateTime CreatedDate { get; set; }

        public int CreatedByUserId { get; set; }
    }
}
