using System;
using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPart : ContentPart
    {
        public string CombinationExcludeRegex
        {
            get { return this.Retrieve(x => x.CombinationExcludeRegex); }
            set { this.Store(x => x.CombinationExcludeRegex, value); }
        }

        public bool CombineCdnResources
        {
            get { return this.Retrieve(x => x.CombineCdnResources); }
            set { this.Store(x => x.CombineCdnResources, value); }
        }

        public string ResourceDomain
        {
            get { return this.Retrieve(x => x.ResourceDomain); }
            set { this.Store(x => x.ResourceDomain, value); }
        }

        public bool EnableForAdmin
        {
            get { return this.Retrieve(x => x.EnableForAdmin); }
            set { this.Store(x => x.EnableForAdmin, value); }
        }

        public bool MinifyResources
        {
            get { return this.Retrieve(x => x.MinifyResources); }
            set { this.Store(x => x.MinifyResources, value); }
        }

        public string MinificationExcludeRegex
        {
            get { return this.Retrieve(x => x.MinificationExcludeRegex); }
            set { this.Store(x => x.MinificationExcludeRegex, value); }
        }

        public bool EmbedCssImages
        {
            get { return this.Retrieve(x => x.EmbedCssImages); }
            set { this.Store(x => x.EmbedCssImages, value); }
        }

        public int EmbeddedImagesMaxSizeKB
        {
            get { return this.Retrieve(x => x.EmbeddedImagesMaxSizeKB); }
            set { this.Store(x => x.EmbeddedImagesMaxSizeKB, value); }
        }

        public string EmbedCssImagesStylesheetExcludeRegex
        {
            get { return this.Retrieve(x => x.EmbedCssImagesStylesheetExcludeRegex); }
            set { this.Store(x => x.EmbedCssImagesStylesheetExcludeRegex, value); }
        }

        public bool GenerateImageSprites
        {
            get { return this.Retrieve(x => x.GenerateImageSprites); }
            set { this.Store(x => x.GenerateImageSprites, value); }
        }

        public string ResourceSetRegexes
        {
            get { return this.Retrieve(x => x.ResourceSetRegexes); }
            set { this.Store(x => x.ResourceSetRegexes, value); }
        }

        public IEnumerable<string> ResourceSetRegexesEnumerable
        {
            get { return ResourceSetRegexes.Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries); }
        }

        private readonly LazyField<int> _cacheFileCount = new LazyField<int>();
        internal LazyField<int> CacheFileCountField { get { return _cacheFileCount; } }
        public int CacheFileCount
        {
            get { return _cacheFileCount.Value; }
        }
    }
}