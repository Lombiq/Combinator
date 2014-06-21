using Orchard.Caching;
using Orchard.Caching.Services;
using Orchard.Environment.Extensions;
using Piedone.HelpfulLibraries.Utilities;

namespace Piedone.Combinator.EventHandlers
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorEventMonitor : ICombinatorEventMonitor, ICombinatorEventHandler
    {
        private readonly ICacheService _cacheService;

        
        public CombinatorEventMonitor(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }


        public void MonitorConfigurationChanged(string cacheKey)
        {
            _cacheService.Monitor("Piedone.Combinator.ConfigurationChanged", cacheKey);
        }

        public void MonitorCacheEmptied(string cacheKey)
        {
            _cacheService.Monitor("Piedone.Combinator.CacheEmptied", cacheKey);
        }

        public void MonitorBundleChanged(string cacheKey, string fingerprint)
        {
            _cacheService.Monitor("Piedone.Combinator.BundleChanged." + fingerprint, cacheKey);
        }

        public void ConfigurationChanged()
        {
            _cacheService.Trigger("Piedone.Combinator.ConfigurationChanged");
        }

        public void CacheEmptied()
        {
            _cacheService.Trigger("Piedone.Combinator.CacheEmptied");
        }

        public void BundleChanged(string fingerprint)
        {
            _cacheService.Trigger("Piedone.Combinator.BundleChanged." + fingerprint);
        }
    }
}