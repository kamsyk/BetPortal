namespace BpDbLayer.Models.Interfaces
{
    internal interface IModified
    {
        public DateTime Modified { get; set; }

        public int ModifiedBy { get; set; }
    }
}
