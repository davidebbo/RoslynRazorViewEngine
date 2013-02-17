using System.Web;
using System.Web.Mvc;

namespace Mvc4ApplicationRoslyn
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}