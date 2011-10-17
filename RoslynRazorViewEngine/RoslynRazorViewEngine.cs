using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.WebPages;
using System.Web.WebPages.Razor;
using System.Web.Razor;
using System.Web.Hosting;
using System.IO;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers;
using System.Globalization;
using System.Web;
using System.Web.Compilation;

namespace RoslynRazorViewEngine {
    public class RoslynRazorViewEngine : VirtualPathProviderViewEngine, IVirtualPathFactory {
        public RoslynRazorViewEngine() {
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
            virtualPath = VirtualPathUtility.ToAbsolute(virtualPath);

            WebPageRazorHost host = WebRazorHostFactory.CreateHostFromConfig(virtualPath);

            RazorTemplateEngine engine = new RazorTemplateEngine(host);
            GeneratorResults results = null;
            using (var stream = VirtualPathProvider.OpenFile(virtualPath)) {
                using (TextReader reader = new StreamReader(stream)) {
                    results = engine.GenerateCode(reader, className: null, rootNamespace: null, sourceFileName: host.PhysicalPath);
                }
            }
            //if (!results.Success) {
            //    throw CreateExceptionFromParserError(results.ParserErrors.Last(), VirtualPath);
            //}
            //_generatedCode = results.GeneratedCode;

            var codeDomProvider = new CSharpCodeProvider();
            var srcFileWriter = new StringWriter();
            codeDomProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, srcFileWriter, new CodeGeneratorOptions());

            string code = srcFileWriter.ToString();
            var syntaxTree = SyntaxTree.ParseCompilationUnit(code);

            var references = new List<AssemblyFileReference>();
            foreach (Assembly referencedAssembly in BuildManager.GetReferencedAssemblies()) {
                references.Add(new AssemblyFileReference(referencedAssembly.Location));
            }

            var compilationOptions = new CompilationOptions(assemblyKind: AssemblyKind.DynamicallyLinkedLibrary);

            var compilation = Compilation.Create("qqq", compilationOptions, new[] { syntaxTree }, references.ToArray());

            var memStream = new MemoryStream();
            var emitResult = compilation.Emit(memStream);

            if (!emitResult.Success) {
                foreach (Diagnostic diagnostic in emitResult.Diagnostics) {
                    throw new Exception(diagnostic.Info.ToString());
                }
                return null;
            }

            var assembly = Assembly.Load(memStream.GetBuffer());

            return assembly.GetType(String.Format(CultureInfo.CurrentCulture, "{0}.{1}", host.DefaultNamespace, host.DefaultClassName));
        }
    }
}
