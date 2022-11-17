namespace Microsoft.AspNetCore.Authentication
{
    internal static class OidcConstants
    {
        internal const string AdditionalClaims = "claims";
        internal const string ScopeUserRead = "user.read";
        internal const string ScopeOpenId = "openid";
        internal const string ScopeProfile = "profile";
        internal const string ScopeOfflineAccess = "offline_access";


        internal const string SchemaTenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
        internal const string SchemaObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    }
}