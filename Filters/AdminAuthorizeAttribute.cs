using System.Web.Mvc;
using System.Web.Routing;

namespace quanlySukien.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;

            // Chưa đăng nhập hoặc không phải Admin
            if (session["UserId"] == null ||
                session["Role"] == null ||
                session["Role"].ToString() != "admin")
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "area", "" },
                        { "controller", "Account" },
                        { "action", "Login" }
                    }
                );

                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}