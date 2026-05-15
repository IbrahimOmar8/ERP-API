using Hangfire.Dashboard;

namespace ERPTask.Auth
{
    // Restricts Hangfire's web dashboard to users authenticated as Admin.
    public class HangfireAdminAuthorization : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();
            return http.User.Identity?.IsAuthenticated == true
                && http.User.IsInRole(Domain.Enums.Roles.Admin);
        }
    }
}
