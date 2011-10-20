using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.WebPages;
using System.Web.WebPages.Razor;
using Microsoft.CSharp;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace RoslynRazorViewEngine {
    public class RoslynRazorViewEngine : VirtualPathProviderViewEngine, IVirtualPathFactory {
        public RoslynRazorViewEngine() {
            base.AreaViewLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml"
            };

            base.AreaMasterLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml"
            };

            base.AreaPartialViewLocationFormats = new[] {
                "~/Areas/{2}/Views/{1}/{0}.cshtml", 
                "~/Areas/{2}/Views/Shared/{0}.cshtml"
            };
            base.ViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/Shared/{0}.cshtml"
            };
            base.MasterLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/Shared/{0}.cshtml"
            };
            base.PartialViewLocationFormats = new[] {
                "~/Views/{1}/{0}.cshtml", 
                "~/Views/Shared/{0}.cshtml"
            };
            base.FileExtensions = new[] {
                "cshtml"
            };
        }

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath) {
            Type type = GetTypeFromVirtualPath(partialPath);
            return new RoslynRazorView(partialPath, type, false, base.FileExtensions);
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath) {
            Type type = GetTypeFromVirtualPath(viewPath);
            return new RoslynRazorView(viewPath, type, true, base.FileExtensions);
        }

        public object CreateInstance(string virtualPath) {
            Type type = GetTypeFromVirtualPath(virtualPath);
            return Activator.CreateInstance(type);
        }

        public bool Exists(string virtualPath) {
            return FileExists(controllerContext: null, virtualPath: virtualPath);
        }

        private Type GetTypeFromVirtualPath(string virtualPath) {

            virtualPath = VirtualPathUtility.ToAbsolute(virtualPath);

            string cacheKey = "RoslynRazor_" + virtualPath;

            Type type = (Type)HttpRuntime.Cache[cacheKey];

            if (type == null) {
                DateTime utcStart = DateTime.UtcNow;

                type = GetTypeFromVirtualPathNoCache(virtualPath);

                // Cache it, and make it dependent on the razor file
                CacheDependency cacheDependency = HostingEnvironment.VirtualPathProvider.GetCacheDependency(virtualPath, new string[] { virtualPath }, utcStart);
                HttpRuntime.Cache.Insert(cacheKey, type, cacheDependency);
            }

            return type;
        }

        private Type GetTypeFromVirtualPathNoCache(string virtualPath) {
            WebPageRazorHost host = WebRazorHostFactory.CreateHostFromConfig(virtualPath);
            string code = GenerateCodeFromRazorTemplate(host, virtualPath);

            var assembly = CompileCodeIntoAssembly(code, virtualPath);

            return assembly.GetType(String.Format(CultureInfo.CurrentCulture, "{0}.{1}", host.DefaultNamespace, host.DefaultClassName));
        }

        private string GenerateCodeFromRazorTemplate(WebPageRazorHost host, string virtualPath) {
            host = WebRazorHostFactory.CreateHostFromConfig(virtualPath);
            VirtualPathProvider vpp = HostingEnvironment.VirtualPathProvider;
            var engine = new RazorTemplateEngine(host);
            GeneratorResults results = null;
            VirtualFile file = vpp.GetFile(virtualPath);
            using (var stream = file.Open()) {
                using (TextReader reader = new StreamReader(stream)) {
                    results = engine.GenerateCode(reader, className: null, rootNamespace: null, sourceFileName: host.PhysicalPath);
                }
            }

            if (!results.Success) {
                throw CreateExceptionFromParserError(results.ParserErrors.Last(), virtualPath);
            }

            var codeDomProvider = new CSharpCodeProvider();
            var srcFileWriter = new StringWriter();
            codeDomProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, srcFileWriter, new CodeGeneratorOptions());

            return srcFileWriter.ToString();
        }

        private Assembly CompileCodeIntoAssembly(string code, string virtualPath) {
            // Parse the source file using Roslyn
            var syntaxTree = SyntaxTree.ParseCompilationUnit(code);

            // Add all the references we need for the compilation
            var references = new List<AssemblyFileReference>();
            foreach (Assembly referencedAssembly in BuildManager.GetReferencedAssemblies()) {
                references.Add(new AssemblyFileReference(referencedAssembly.Location));
            }

            var compilationOptions = new CompilationOptions(assemblyKind: AssemblyKind.DynamicallyLinkedLibrary);

            // Note: using a fixed assembly name, which doesn't matter as long as we don't expect cross references of generated assemblies
            var compilation = Compilation.Create("SomeAssemblyName", compilationOptions, new[] { syntaxTree }, references);

            // Generate the assembly into a memory stream
            var memStream = new MemoryStream();
            EmitResult emitResult = compilation.Emit(memStream);

            if (!emitResult.Success) {
                Diagnostic diagnostic = emitResult.Diagnostics.First();
                string message = diagnostic.Info.ToString();
                LinePosition linePosition = diagnostic.Location.GetLineSpan(usePreprocessorDirectives: true).StartLinePosition;

                throw new HttpParseException(message, null, virtualPath, null, linePosition.Line + 1);
            }

            return Assembly.Load(memStream.GetBuffer());
        }

        private HttpParseException CreateExceptionFromParserError(RazorError error, string virtualPath) {
            return new HttpParseException(error.Message + Environment.NewLine, null, virtualPath, null, error.Location.LineIndex + 1);
        }
    }
}
