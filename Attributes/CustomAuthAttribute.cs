using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthSystemApi.Attributes
{
    public class CustomAuthAttribute : Attribute, IAuthorizationFilter
    {
        private readonly List<string> allowedRoles;

        public CustomAuthAttribute(params string[] roles)
        {
            allowedRoles = new List<string>();

            // Default to Admin if no roles provided
            if (roles == null || roles.Length == 0)
                allowedRoles.Add("Admin");
            else
                allowedRoles.AddRange(roles);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

                if (string.IsNullOrEmpty(authHeader))
                {
                    context.Result = new UnauthorizedObjectResult("Authorization header is missing");
                    return;
                }

                var tokenParts = authHeader.Split(' ');
                if (tokenParts.Length != 2 || tokenParts[0] != "Bearer")
                {
                    context.Result = new UnauthorizedObjectResult("Invalid authorization header format");
                    return;
                }

                var token = tokenParts[1];

                //  SECURE VALIDATION
                var principal = ValidateJwtToken(token, context);

                if (principal == null)
                {
                    context.Result = new UnauthorizedObjectResult("Invalid or expired token");
                    return;
                }

                // Extract role
                var role = principal.Claims.FirstOrDefault(c =>
                    c.Type == ClaimTypes.Role ||
                    c.Type == "role" ||
                    c.Type.EndsWith("/claims/role")
                )?.Value;

                if (string.IsNullOrEmpty(role) || !allowedRoles.Contains(role))
                {
                    context.Result = new ForbidResult();
                    return;
                }

                // Attach user to HttpContext (optional but powerful)
                context.HttpContext.User = principal;
            }
            catch
            {
                context.Result = new UnauthorizedObjectResult("Authorization failed");
            }
        }

        private ClaimsPrincipal? ValidateJwtToken(string token, AuthorizationFilterContext context)
        {
            var handler = new JwtSecurityTokenHandler();

            var configuration = context.HttpContext.RequestServices
                .GetService<IConfiguration>();

            var secretKey = configuration!["Jwt:Key"];
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = issuer,
                ValidAudience = audience,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey!)
                ),

                ClockSkew = TimeSpan.Zero // no grace period
            };

            return handler.ValidateToken(token, validationParameters, out _);
        }
    }

  

    public class AdminOnlyAttribute : CustomAuthAttribute
    {
        public AdminOnlyAttribute() : base("Admin") { }
    }

    public class EmployeeOnlyAttribute : CustomAuthAttribute
    {
        public EmployeeOnlyAttribute() : base("Employee") { }
    }

    public class JobSeekerOnlyAttribute : CustomAuthAttribute
    {
        public JobSeekerOnlyAttribute() : base("JobSeeker") { }
    }

    public class AdminOrEmployeeAttribute : CustomAuthAttribute
    {
        public AdminOrEmployeeAttribute() : base("Admin", "Employee") { }
    }

    public class AllRolesAttribute : CustomAuthAttribute
    {
        public AllRolesAttribute() : base("Admin", "Employee", "JobSeeker") { }
    }
}
