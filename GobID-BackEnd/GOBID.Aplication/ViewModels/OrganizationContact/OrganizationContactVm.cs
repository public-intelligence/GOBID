using Newtonsoft.Json;

namespace GOBID.Application.ViewModels.OrganizationContact;

public class OrganizationContactVm
{
    public int ContactId { get; set; }
    public int OrganizationId { get; set; }
    public required string OrganizationContactName { get; set; }
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}