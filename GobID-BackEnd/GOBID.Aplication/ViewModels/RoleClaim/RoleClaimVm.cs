using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GOBID.Application.ViewModels.RoleClaim;

public class RoleClaimVm
{
    public int RoleClaimId { get; set; }

    public int RoleId { get; set; }

    public int ClaimId { get; set; }
    public required string ClaimName { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

}
