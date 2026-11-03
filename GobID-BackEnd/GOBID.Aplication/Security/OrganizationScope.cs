namespace GOBID.Application.Security;

/// <summary>
/// Alcance por organización de los endpoints que listan datos de una organización indicada en la ruta (G1).
/// <list type="bullet">
/// <item>Un token de usuario solo puede consultar la organización de su claim <c>organization_id</c>.</item>
/// <item>Una cuenta de servicio (M2M) puede consultar además las organizaciones donde su aplicación está
/// instalada y activa: un sistema como SDS atiende varias instituciones con una sola credencial.</item>
/// <item><c>super_admin</c> puede consultar cualquiera.</item>
/// </list>
/// </summary>
public static class OrganizationScope
{
    public const string SuperAdminPermission = "super_admin";

    public static bool CanAccessOrganization(
        int tokenOrganizationId,
        int requestedOrganizationId,
        IEnumerable<string> permissions,
        bool applicationInstalledInRequestedOrganization = false)
    {
        if (permissions.Contains(SuperAdminPermission))
            return true;

        if (tokenOrganizationId > 0 && tokenOrganizationId == requestedOrganizationId)
            return true;

        return applicationInstalledInRequestedOrganization;
    }
}
