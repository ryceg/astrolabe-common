using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Astrolabe.Web.Common;

public class HtmlFileProvider(string rootPath) : IFileProvider
{
    private readonly PhysicalFileProvider _provider = new(rootPath);

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
        return resolvedParent + (dynamic != null ? Path.GetFileNameWithoutExtension(dynamic.Name) : filename);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        var extension = Path.GetExtension(subpath);
        return extension == "" ? _provider.GetFileInfo(ResolveDynamic(subpath) + ".html") : _provider.GetFileInfo(subpath);
    }

    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }
}