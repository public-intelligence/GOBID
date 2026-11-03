namespace GOBID.Aplication.ViewModels.Representative
{
    public class RepresentativeVm
    {
        public int RepresentativeId { get; set; }

        public string? RepresentativeName { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
