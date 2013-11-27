using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.FieldStorage.InfosetStorage;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using Orchard.Settings;
using Upgrade.Services;
using Orchard.ContentManagement;
using Piedone.Combinator.Models;

namespace Piedone.Combinator
{
    [OrchardFeature("Piedone.Combinator.Upgrade")]
    public class UpgradeMigrations : DataMigrationImpl
    {
        private readonly IUpgradeService _upgradeService;
        private readonly ISiteService _siteService;


        public UpgradeMigrations(IUpgradeService upgradeService, ISiteService siteService)
        {
            _upgradeService = upgradeService;
            _siteService = siteService;
        }
	
			
        public int Create()
        {
            _upgradeService.ExecuteReader("SELECT * FROM " + _upgradeService.GetPrefixedTableName("Piedone_Combinator_CombinatorSettingsPartRecord"),
                (reader, connection) =>
                {
                    var part = _siteService.GetSiteSettings().As<CombinatorSettingsPart>();

                    part.CombinationExcludeRegex = (string)reader["CombinationExcludeRegex"];
                    part.CombineCdnResources = (bool)reader["CombineCdnResources"];
                    part.ResourceDomain = (string)reader["ResourceDomain"];
                    part.EnableForAdmin = (bool)reader["EnableForAdmin"];
                    part.MinifyResources = (bool)reader["MinifyResources"];
                    part.MinificationExcludeRegex = (string)reader["MinificationExcludeRegex"];
                    part.EmbedCssImages = (bool)reader["EmbedCssImages"];
                    part.EmbeddedImagesMaxSizeKB = (int)reader["EmbeddedImagesMaxSizeKB"];
                    part.EmbedCssImagesStylesheetExcludeRegex = (string)reader["EmbedCssImagesStylesheetExcludeRegex"];
                    part.GenerateImageSprites = (bool)reader["GenerateImageSprites"];
                    part.ResourceSetRegexes = (string)reader["ResourceSetRegexes"];
                });

            _upgradeService.ExecuteReader("DROP TABLE " + _upgradeService.GetPrefixedTableName("Piedone_Combinator_CombinatorSettingsPartRecord"), null);


            return 1;
        }
    }
}