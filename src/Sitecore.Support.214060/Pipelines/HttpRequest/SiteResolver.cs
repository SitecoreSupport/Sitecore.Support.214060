using System;
using System.IO;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Sites;
using Sitecore.Web;

namespace Sitecore.Support.Pipelines.HttpRequest
{
  public class SiteResolver : HttpRequestProcessor
  {
    protected string GetFilePath(HttpRequestArgs args, SiteContext context) =>
        this.GetPath(context.PhysicalFolder, args.Url.FilePath, context);

    protected string GetItemPath(HttpRequestArgs args, SiteContext context) =>
        this.GetPath(context.StartPath, args.Url.ItemPath, context);

    protected string GetPath(string basePath, string path, SiteContext context)
    {
      string virtualFolder = context.VirtualFolder;
      if ((virtualFolder.Length > 0) && (virtualFolder != "/"))
      {
        string str2 = StringUtil.EnsurePostfix('/', virtualFolder);
        if (StringUtil.EnsurePostfix('/', path).StartsWith(str2, StringComparison.InvariantCultureIgnoreCase))
        {
          path = StringUtil.Mid(path, str2.Length);
        }
      }
      if ((basePath.Length > 0) && (basePath != "/"))
      {
        path = FileUtil.MakePath(basePath, path, '/');
      }
      if ((path.Length > 0) && (path[0] != '/'))
      {
        path = '/' + path;
      }
      return path;
    }

    public override void Process(HttpRequestArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      SiteContext site = this.ResolveSiteContext(args);
      this.UpdatePaths(args, site);
      this.SetSiteToRequestContext(site);
    }

    protected virtual SiteContext ResolveSiteContext(HttpRequestArgs args)
    {
      SiteContext siteContext;
      string queryString = WebUtil.GetQueryString("sc_site");
      if (queryString.Length > 0)
      {
        siteContext = SiteContextFactory.GetSiteContext(queryString);
        Assert.IsNotNull(siteContext, "Site from query string was not found: " + queryString);
        return siteContext;
      }
      if (Settings.EnableSiteConfigFiles)
      {
        string path = FileUtil.MakePath(FileUtil.NormalizeWebPath(StringUtil.GetString(new string[] { Path.GetDirectoryName(args.Url.FilePath) })), "site.config");
        if (FileUtil.Exists(path))
        {
          siteContext = SiteContextFactory.GetSiteContextFromFile(path);
          Assert.IsNotNull(siteContext, "Site from site.config was not found: " + path);
          return siteContext;
        }
      }
      Uri requestUri = WebUtil.GetRequestUri();
      siteContext = SiteContextFactory.GetSiteContext(requestUri.Host, args.Url.FilePath, requestUri.Port);
      Assert.IsNotNull(siteContext, "Site from host name and path was not found. Host: " + requestUri.Host + ", path: " + args.Url.FilePath);
      return siteContext;
    }

    protected virtual void UpdatePaths(HttpRequestArgs args, SiteContext site)
    {
      if (!string.IsNullOrEmpty(args.Context.Request.PathInfo))
      {
        string filePath = args.Url.FilePath;
        int length = filePath.LastIndexOf('.');
        int num2 = filePath.LastIndexOf('/');
        args.Url.ItemPath = (length >= 0) ? ((length >= num2) ? filePath.Substring(0, length) : filePath) : filePath;
      }
      args.StartPath = site.StartPath;
      args.Url.ItemPath = this.GetItemPath(args, site);
      site.Request.ItemPath = args.Url.ItemPath;
      args.Url.FilePath = this.GetFilePath(args, site);
      site.Request.FilePath = args.Url.FilePath;
    }

    /// <summary>
    /// Sets the resolved site to request context.
    /// </summary>
    /// <param name="site">The site to be used in current request.</param>
    protected virtual void SetSiteToRequestContext([NotNull]SiteContext site)
    {
      Context.Site = site;
      if (Sitecore.Context.PageMode.IsNormal)
      {
        Context.SetLanguage(Language.Parse(site.Language), false);
      }
    }
  }
}
