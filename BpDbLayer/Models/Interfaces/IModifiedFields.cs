namespace BpDbLayer.Models.Interfaces
{
    internal interface IModifiedFields
    {
        public DateTime ModifiedDate { get; set; }

        public int ModifiedByUserId { get; set; }
    }
}
