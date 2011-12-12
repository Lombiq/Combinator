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
        private readonly ITaskFactory _taskFactory;

        public ResourceProcessingService(
            IResourceFileService resourceFileService,
            IMinificationService minificationService,
            ITaskFactory taskFactory)
        {
            _resourceFileService = resourceFileService;
            _minificationService = minificationService;
            _taskFactory = taskFactory;
        }

        public void ProcessResource(ISmartResource resource, StringBuilder combinedContent, ICombinatorSettings settings)
        {
            if (!resource.IsCDNResource || settings.CombineCDNResources)
            {
                if (!resource.IsCDNResource)
                {
                    resource.Content = _resourceFileService.GetLocalResourceContent(resource);
                }
                else if (settings.CombineCDNResources)
                {
                    resource.Content = _resourceFileService.GetRemoteResourceContent(resource);
                }

                if (settings.MinifyResources && (String.IsNullOrEmpty(settings.MinificationExcludeRegex) || !Regex.IsMatch(resource.PublicUrl.ToString(), settings.MinificationExcludeRegex)))
                {
                    MinifyResourceContent(resource);
                }

                // Better to do after minification, as then urls commented out are removed
                if (resource.Type == ResourceType.Style)
                {
                    AdjustRelativePaths(resource);

                    if (settings.EmbedCssImages)
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
                        var imageData = _resourceFileService.GetImageBase64Data(new Uri(url), maxSizeKB);

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

                    return "url(\"" + new Uri(resource.PublicUrl, url) + "\")";
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