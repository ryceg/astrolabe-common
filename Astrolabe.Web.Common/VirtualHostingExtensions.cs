using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Astrolabe.Web.Common;

public static class VirtualHostingExtensions
{
    public static IApplicationBuilder UseDomainSpa(
        this IApplicationBuilder app,
        IWebHostEnvironment env,
        string siteDir,
        string? domainPrefix = null,
        bool fallback = false
    )
    {
        domainPrefix ??= siteDir + ".";
        var buildDir = Path.Combine(env.ContentRootPath, "ClientApp/sites", siteDir, "out");
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        var fileOptions = new StaticFileOptions()
        {
            FileProvider = new HtmlFileProvider(buildDir),
            RequestPath = "",
            ServeUnknownFileTypes = true,
            DefaultContentType = "text/html;charset=UTF-8"
        };
        app.UseWhen(
            ctx => fallback || ctx.Request.Host.Host.StartsWith(domainPrefix),
            (app2) =>
            {
                app2.UseStaticFiles(fileOptions);

                app2.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "ClientApp";
                    spa.Options.DefaultPageStaticFileOptions = fileOptions;
                });
            }
        );
        return app;
    }

    class HtmlFileProvider : IFileProvider
    {
        private readonly PhysicalFileProvider _provider;

        public HtmlFileProvider(string rootPath)
        {
            _provider = new PhysicalFileProvider(rootPath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _provider.GetDirectoryContents(subpath);
        }

        private string ResolveDynamic(string path)
        {
            var parentPath = Path.GetDirectoryName(path);
            var filename = Path.GetFileName(path);
            if (parentPath == null)
                return path;
            var resolvedParent = ResolveDynamic(parentPath);
            var dirContents = GetDirectoryContents(resolvedParent);
            var dynamic = dirContents.FirstOrDefault(x => x.Name.StartsWith("["));
            if (!resolvedParent.EndsWith("/"))
                resolvedParent += "/";
            return resolvedParent
                + (dynamic != null ? Path.GetFileNameWithoutExtension(dynamic.Name) : filename);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var extension = Path.GetExtension(subpath);
            if (extension == "")
            {
                return _provider.GetFileInfo(ResolveDynamic(subpath) + ".html");
            }
            return _provider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }
    }
}
