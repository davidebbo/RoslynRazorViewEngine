using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.WebPages;

namespace RoslynRazorViewEngine {
    public class RoslynRazorViewEngine : VirtualPathProviderViewEngine, IVirtualPathFactory {
        public RoslynRazorViewEngine(Assembly assembly) {
            base.AreaViewLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/{1}/{0}.vbhtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.vbhtml"
            };

            base.AreaMasterLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/{1}/{0}.vbhtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.vbhtml"
            };

            base.AreaPartialViewLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/{1}/{0}.vbhtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.vbhtml"
            };
            base.ViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/{1}/{0}.vbhtml", 
                "~/Views/Shared/{0}.cshtml", 
                "~/Views/Shared/{0}.vbhtml"
            };
            base.MasterLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/{1}/{0}.vbhtml", 
                "~/Views/Shared/{0}.cshtml", 
                "~/Views/Shared/{0}.vbhtml"
            };
            base.PartialViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/{1}/{0}.vbhtml", 
                "~/Views/Shared/{0}.cshtml", 
                "~/Views/Shared/{0}.vbhtml"
            };
            base.FileExtensions = new[] {
                "cshtml", 
                "vbhtml"
            };
        }

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath) {
            Type type = GetTypeFromVirtualPath(partialPath);
            if (type != null) {
                return new RoslynRazorView(partialPath, type, false, base.FileExtensions);
            }
            return null;
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath) {
            Type type = GetTypeFromVirtualPath(viewPath);
            if (type != null) {
                return new RoslynRazorView(viewPath, type, true, base.FileExtensions);
            }
            return null;
        }

        public object CreateInstance(string virtualPath) {
            Type type = GetTypeFromVirtualPath(virtualPath);

            if (type != null) {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public bool Exists(string virtualPath) {
            return FileExists(controllerContext: null, virtualPath: virtualPath);
        }

        private Type GetTypeFromVirtualPath(string virtualPath) {
            throw new NotImplementedException();
        }
    }
}
