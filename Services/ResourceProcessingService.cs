using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
                        EmbedImages(resource);
                    }
                }

                combinedContent.Append(resource.Content);
            }
            else
            {
                resource.OverrideCombinedUrl(resource.PublicUrl);
            }
        }

        private void EmbedImages(ISmartResource resource)
        {
            // Uri is the key so that the key is uniform, inclusion urls are not
            var imageUrls = new Dictionary<Uri, string>();

            ProcessUrlSettings(resource,
                (match) =>
                {
                    var url = match.Groups[1].ToString();
                    var extension = Path.GetExtension(url).ToLowerInvariant();

                    // This is a dumb check but otherwise we'd have to inspect the file thoroughly
                    if (!String.IsNullOrEmpty(extension) && ".jpg .jpeg .png .gif .tiff .bmp".Contains(extension))
                    {
                        imageUrls[new Uri(url)] = url;
                    }

                    return match.Groups[0].ToString();
                });


            if (imageUrls.Count != 0)
            {
                var dataUrls = new ConcurrentBag<Tuple<string, string>>();
                Task[] tasks = new Task[imageUrls.Count];

                var downloaderAction = _taskFactory.BuildTaskAction(
                    (urlObject) =>
                    {
                        var url = (KeyValuePair<Uri, string>)urlObject;
                        try
                        {
                            dataUrls.Add(new Tuple<string, string>(
                                url.Value,
                                "data:image/"
                                    + Path.GetExtension(url.Key.ToString()).Replace(".", "")
                                    + ";base64,"
                                    + _resourceFileService.GetImageBase64Data(url.Key)));
                        }
                        catch (Exception e)
                        {
                            throw new ApplicationException("The image with url " + url.Value + " can't be embedded", e);
                        }
                    }, false);


                int taskIndex = 0;
                foreach (var imageUrl in imageUrls)
                {
                    tasks[taskIndex++] = Task.Factory.StartNew(downloaderAction, imageUrl);
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (AggregateException e)
                {
                    throw new ApplicationException("Embedding image files into css failed.", e);
                }


                foreach (var url in dataUrls)
                {
                    resource.Content = resource.Content.Replace(url.Item1, url.Item2);
                }
            }
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