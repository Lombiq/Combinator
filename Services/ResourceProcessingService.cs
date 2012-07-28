using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Models;
using Piedone.Combinator.SpriteGenerator;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;

namespace Piedone.Combinator.Services
{
    public class ResourceProcessingService : IResourceProcessingService
    {
        private readonly IResourceFileService _resourceFileService;
        private readonly IMinificationService _minificationService;
        private readonly ICacheFileService _cacheFileService;

        private delegate string ImageMatchProcessor(string url, string extension, Match match);

        public ResourceProcessingService(
            IResourceFileService resourceFileService,
            IMinificationService minificationService,
            ICacheFileService cacheFileService)
        {
            _resourceFileService = resourceFileService;
            _minificationService = minificationService;
            _cacheFileService = cacheFileService;
        }

        public void ProcessResource(CombinatorResource resource, StringBuilder combinedContent, ICombinatorSettings settings)
        {
            if (!resource.IsCdnResource || settings.CombineCDNResources)
            {
                var absoluteUrlString = resource.AbsoluteUrl.ToString();

                if (!resource.IsCdnResource || settings.CombineCDNResources)
                {
                    _resourceFileService.LoadResourceContent(resource);
                }

                if (String.IsNullOrEmpty(resource.Content)) return;

                if (settings.MinifyResources && (settings.MinificationExcludeFilter == null || !settings.MinificationExcludeFilter.IsMatch(absoluteUrlString)))
                {
                    MinifyResourceContent(resource);
                    if (String.IsNullOrEmpty(resource.Content)) return;
                }

                // Better to do after minification, as then urls commented out are removed
                if (resource.Type == ResourceType.Style)
                {
                    AdjustRelativePaths(resource);

                    if (settings.EmbedCssImages && (settings.EmbedCssImagesStylesheetExcludeFilter == null || !settings.EmbedCssImagesStylesheetExcludeFilter.IsMatch(absoluteUrlString)))
                    {
                        EmbedImages(resource, settings.EmbeddedImagesMaxSizeKB);
                    }
                }

                combinedContent.Append(resource.Content);
            }
            else
            {
                resource.IsOriginal = true;
            }
        }

        public void ReplaceCssImagesWithSprite(CombinatorResource resource)
        {
            var images = new Dictionary<string, CssImage>();

            ProcessImageUrls(resource,
                (url, extension, match) =>
                {
                    var imageContent = _resourceFileService.GetImageContent(MakeInlineUri(resource, url), 5000);

                    if (imageContent != null)
                    {
                        images[url] = new CssImage { Content = imageContent };
                    }

                    return null;
                });

            if (images.Count == 0) return;

            _cacheFileService.WriteSpriteStream(
                resource.Content.GetHashCode() + ".jpg",
                (stream, publicUrl) =>
                {
                    using (var sprite = new Sprite(images.Values.Select(image => image.Content)))
                    {
                        var imageEnumerator = images.Values.GetEnumerator();
                        foreach (var backgroundImage in sprite.Generate(stream, ImageFormat.Jpeg))
                        {
                            imageEnumerator.MoveNext();
                            imageEnumerator.Current.BackgroundImage = backgroundImage;
                            imageEnumerator.Current.BackgroundImage.ImageUrl = publicUrl;
                        }
                    }
                });

            resource.Content = Regex.Replace(
                resource.Content,
                @"background-image:\s?url\(['|""]?(.+?)['|""]?\);?",
                (match) =>
                {
                    return images[match.Groups[1].Value].BackgroundImage.ToString();
                },
                RegexOptions.IgnoreCase);
        }

        private class CssImage
        {
            public byte[] Content { get; set; }
            public BackgroundImage BackgroundImage { get; set; }
        }

        private void ProcessImageUrls(CombinatorResource resource, ImageMatchProcessor matchProcessor)
        {
            ProcessUrls(resource,
                (match) =>
                {
                    var url = match.Groups[1].Value;
                    var extension = Path.GetExtension(url).ToLowerInvariant();

                    // This is a dumb check but otherwise we'd have to inspect the file thoroughly
                    if (!String.IsNullOrEmpty(extension) && ".jpg .jpeg .png .gif .tiff .bmp".Contains(extension))
                    {
                        var result = matchProcessor(url, extension, match);
                        if (result != null) return result;
                    }

                    return match.Groups[0].Value;
                });
        }

        private void EmbedImages(CombinatorResource resource, int maxSizeKB)
        {
            ProcessImageUrls(resource,
                (url, extenstion, match) =>
                {
                    var imageData = _resourceFileService.GetImageContent(MakeInlineUri(resource, url), maxSizeKB);

                    if (imageData != null)
                    {
                        var dataUrl =
                        "data:image/"
                            + Path.GetExtension(url).Replace(".", "")
                            + ";base64,"
                            + Convert.ToBase64String(imageData);

                        return "url(\"" + dataUrl + "\")";
                    }

                    return null;
                });
        }

        private static void AdjustRelativePaths(CombinatorResource resource)
        {
            ProcessUrls(resource,
                (match) =>
                {
                    var url = match.Groups[1].ToString();

                    var uri = MakeInlineUri(resource, url);

                    // Remote paths are preserved as full urls, local paths become uniformed relative ones.
                    string uriString = "";
                    if (resource.IsCdnResource || resource.AbsoluteUrl.Host != uri.Host) uriString = uri.ToStringWithoutScheme();
                    else uriString = uri.PathAndQuery;

                    return "url(\"" + uriString + "\")";
                });
        }

        private static void ProcessUrls(CombinatorResource resource, MatchEvaluator evaluator)
        {
            string content = resource.Content;

            content = Regex.Replace(
                                    content,
                                    "url\\(['|\"]?(.+?)['|\"]?\\)",
                                    evaluator,
                                    RegexOptions.IgnoreCase);

            resource.Content = content;
        }

        private static Uri MakeInlineUri(CombinatorResource resource, string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute) ? new Uri(url) : new Uri(resource.AbsoluteUrl, url);
        }

        private void MinifyResourceContent(CombinatorResource resource)
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