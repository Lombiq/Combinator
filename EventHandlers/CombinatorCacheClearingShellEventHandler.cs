using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.AppData;
using Piedone.Combinator.Constants;
using Piedone.Combinator.Services;

namespace Piedone.Combinator.EventHandlers
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorCacheClearingShellEventHandler : IOrchardShellEvents
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly IAppDataFolder _appDataFolder;
        private readonly ShellSettings _shellSettings;

        /// <summary>
        /// Gets or sets a value indicating whether emptying Combinator cache is disabled, primarily through HostComponents.config.
        /// </summary>
        public bool IsDisabled { get; set; } = true;

        public CombinatorCacheClearingShellEventHandler(
            ICacheFileService cacheFileService,
            IAppDataFolder appDataFolder,
            ShellSettings shellSettings)
        {
            _cacheFileService = cacheFileService;
            _appDataFolder = appDataFolder;
            _shellSettings = shellSettings;
        }

        public void Activated()
        {
            if (IsDisabled) return;

            var pathToClearCacheFile = _appDataFolder.Combine(
                FolderNames.Sites, _shellSettings.Name, FolderNames.PiedoneModules, FolderNames.Combinator, FileNames.ClearCache);

            if (!_appDataFolder.FileExists(pathToClearCacheFile)) return;

            _cacheFileService.Empty();

            _appDataFolder.DeleteFile(pathToClearCacheFile);
        }

        public void Terminating()
        {
            // Terminating event does not need to be implemented.
        }
    }
}
