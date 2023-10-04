using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.AppData;
using Piedone.Combinator.Constants;
using Piedone.Combinator.Services;

namespace Piedone.Combinator.EventHandlers
{
    [OrchardFeature("Piedone.Combinator")]
    public class CacheFileEventHandler : IOrchardShellEvents
    {
        private const string ClearCacheFileName = FileNames.ClearCache + ".txt";

        private readonly ICacheFileService _cacheFileService;
        private readonly IAppDataFolder _appDataFolder;

        private readonly string _basePath;

        /// <summary>
        /// Gets or sets a value indicating whether emptying Combinator cache is disabled, primarily through HostComponents.config.
        /// </summary>
        public bool IsEmptyingCacheDisabled { get; set; }

        public CacheFileEventHandler(ICacheFileService cacheFileService, IAppDataFolder appDataFolder, ShellSettings shellSettings)
        {
            _cacheFileService = cacheFileService;
            _appDataFolder = appDataFolder;

            _basePath = _appDataFolder.Combine(FolderNames.Sites, shellSettings.Name, FolderNames.PiedoneModules, FolderNames.Combinator);
        }

        public void Activated()
        {
            if (IsEmptyingCacheDisabled) return;

            var pathToClearCacheFile = _appDataFolder.Combine(_basePath, ClearCacheFileName);

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
