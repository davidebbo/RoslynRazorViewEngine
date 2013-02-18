using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(Mvc4ApplicationRoslyn.App_Start.RoslynRazorViewEngineStart), "Start")]

namespace Mvc4ApplicationRoslyn.App_Start
{
    public class RoslynRazorViewEngineStart
    {
        public static void Start()
        {
            var engine = new RoslynRazorViewEngine.RoslynRazorViewEngine();

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(engine);

            // StartPage lookups are done by WebPages. 
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);
        }
    }
}