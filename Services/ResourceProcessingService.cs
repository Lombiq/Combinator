using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Models;
using Piedone.HelpfulLibraries.Tasks;

namespace Piedone.Combinator.Services
{
    public class ResourceProcessingService : IResourceProcessingService
    {
        private readonly IResourceFileService _resourceFileService;
        private readonly IMinificationService _minificationService;

        public ResourceProcessingService(
            IResourceFileService resourceFileService,
            IMinificationService minificationService)
        {
            _resourceFileService = resourceFileService;
            _minificationService = minificationService;
        }

        public void ProcessResource(ISmartResource resource, StringBuilder combinedContent, ICombinatorSettings settings)
        {
            if (!resource.IsCDNResource || settings.CombineCDNResources)
            {
                var publicUrl = resource.PublicUrl.ToString();

                if (!resource.IsCDNResource)
                {
                    resource.Content = _resourceFileService.GetLocalResourceContent(resource);
                }
                else if (settings.CombineCDNResources)
                {
                    resource.Content = _resourceFileService.GetRemoteResourceContent(resource);
                }

                if (String.IsNullOrEmpty(resource.Content)) return;

                if (settings.MinifyResources && (String.IsNullOrEmpty(settings.MinificationExcludeRegex) || !Regex.IsMatch(publicUrl, settings.MinificationExcludeRegex)))
                {
                    MinifyResourceContent(resource);
                    if (String.IsNullOrEmpty(resource.Content)) return;
                }

                // Better to do after minification, as then urls commented out are removed
                if (resource.Type == ResourceType.Style)
                {
                    AdjustRelativePaths(resource);

                    if (settings.EmbedCssImages && (String.IsNullOrEmpty(settings.EmbedCssImagesStylesheetExcludeRegex) || !Regex.IsMatch(publicUrl, settings.EmbedCssImagesStylesheetExcludeRegex)))
                    {
                        EmbedImages(resource, settings.EmbeddedImagesMaxSizeKB);
                    }
                }

                combinedContent.Append(resource.Content);
            }
            else
            {
                resource.OverrideCombinedUrl(resource.PublicUrl);
            }
        }

        private void EmbedImages(ISmartResource resource, int maxSizeKB)
        {
            ProcessUrlSettings(resource,
                (match) =>
                {
                    var url = match.Groups[1].Value;
                    var extension = Path.GetExtension(url).ToLowerInvariant();

                    // This is a dumb check but otherwise we'd have to inspect the file thoroughly
                    if (!String.IsNullOrEmpty(extension) && ".jpg .jpeg .png .gif .tiff .bmp".Contains(extension))
                    {
                        var imageData = _resourceFileService.GetImageBase64Data(MakeInlineUri(resource, url), maxSizeKB);

                        if (!String.IsNullOrEmpty(imageData))
                        {
                            var dataUrl =
                            "data:image/"
                                + Path.GetExtension(url).Replace(".", "")
                                + ";base64,"
                                + imageData;

                            return "url(\"" + dataUrl + "\")"; 
                        }
                    }

                    return match.Groups[0].Value;
                });
        }

        private static void AdjustRelativePaths(ISmartResource resource)
        {
            ProcessUrlSettings(resource,
                (match) =>
                {
                    var url = match.Groups[1].ToString();

                    var uri = MakeInlineUri(resource, url);

                    // Remote paths are preserved as full urls, local paths become uniformed relative ones.
                    string uriString = "";
                    if (resource.IsCDNResource || resource.PublicUrl.Host != uri.Host) uriString = uri.ToString();
                    else uriString = uri.PathAndQuery;

                    return "url(\"" + uriString + "\")";
                });
        }

        private static void ProcessUrlSettings(ISmartResource resource, MatchEvaluator evaluator)
        {
            string content = resource.Content;

            content = Regex.Replace(
                                    content,
                                    "url\\(['|\"]?(.+?)['|\"]?\\)",
                                    evaluator,
                                    RegexOptions.IgnoreCase);

            resource.Content = content;
        }

        private static Uri MakeInlineUri(ISmartResource resource, string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute) ? new Uri(url) : new Uri(resource.PublicUrl, url);
        }

        private void MinifyResourceContent(ISmartResource resource)
        {
            if (resource.Type == ResourceType.Style)
            {
                resource.Content = _minificationService.MinifyCss(resource.Content);
            }
            else if (resource.Type == ResourceType.JavaScript)
            {
                resource.Content = _minificationService.MinifyJavaScript(resource.Content);
            }
        }
    }
}