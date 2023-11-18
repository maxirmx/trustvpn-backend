
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace o_service_api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // skip authorization if action is decorated with [AllowAnonymous] attribute
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (allowAnonymous)
            return;

        // authorization
        var userId = (int?)context.HttpContext.Items["UserId"];
        if (userId == null) {
            Console.WriteLine("Not logged in or role not authorized");
            context.Result = new JsonResult(new { message = "Необходимо войти в систему." }) { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }
}
