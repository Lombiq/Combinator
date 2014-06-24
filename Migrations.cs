using System;
using System.Data;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;

namespace Piedone.Combinator.Migrations
{
    [OrchardFeature("Piedone.Combinator")]
    public class Migrations : DataMigrationImpl
    {
        private readonly ICacheFileService _cacheFileService;


        public Migrations(ICacheFileService cacheFileService)
        {
            _cacheFileService = cacheFileService;
        }


        public int Create()
        {
            SchemaBuilder.CreateTable(typeof(CombinedFileRecord).Name,
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<string>("Fingerprint", column => column.NotNull().WithLength(1024))
                    .Column<int>("Slice")
                    .Column<string>("Type")
                    .Column<DateTime>("LastUpdatedUtc")
                    .Column<string>("Settings", column => column.Unlimited())
                )
            .AlterTable(typeof(CombinedFileRecord).Name,
                table => table
                    .CreateIndex("FileFingerprint", new[] { "Fingerprint" })
            );


            return 13;
        }

        public int UpdateFrom1()
        {
            SchemaBuilder.AlterTable("CombinatorSettingsPartRecord",
                table =>
                {
                    table.AddColumn<bool>("MinifyResources");
                    table.AddColumn<string>("MinificationExcludeRegex");
                }
            );


            return 2;
        }

        public int UpdateFrom2()
        {
            SchemaBuilder.AlterTable("CombinatorSettingsPartRecord",
                table => table
                    .AddColumn<string>("CombinationExcludeRegex")
                );

            SchemaBuilder.AlterTable(typeof(CombinedFileRecord).Name,
                table => table
                    .AddColumn<string>("Settings", column => column.Unlimited())
                );

            return 3;
        }

        public int UpdateFrom3()
        {
            SchemaBuilder.AlterTable("CombinatorSettingsPartRecord",
                table =>
                {
                    table.AddColumn<bool>("EmbedCssImages");
                    table.AddColumn<int>("EmbeddedImagesMaxSizeKB");
                    table.AddColumn<string>("EmbedCssImagesStylesheetExcludeRegex");
                }
            );


            return 4;
        }

        public int UpdateFrom4()
        {
            SchemaBuilder.AlterTable("CombinatorSettingsPartRecord",
                table => table
                    .AddColumn<string>("ResourceSetRegexes")
                );

            return 5;
        }

        public int UpdateFrom5()
        {
            SchemaBuilder.AlterTable("CombinatorSettingsPartRecord",
                table => table
                    .AddColumn<bool>("EnableForAdmin")
                );

            return 6;
        }

        public int UpdateFrom6()
        {
            SchemaBuilder.AlterTable("CombinatorSettingsPartRecord",
                table =>
                {
                    table.AlterColumn("CombinationExcludeRegex", column => column.WithType(DbType.String).Unlimited());
                    table.AlterColumn("MinificationExcludeRegex", column => column.WithType(DbType.String).Unlimited());
                    table.AlterColumn("EmbedCssImagesStylesheetExcludeRegex", column => column.WithType(DbType.String).Unlimited());
                    table.AlterColumn("ResourceSetRegexes", column => column.WithType(DbType.String).Unlimited());
                }
            );


            return 7;
        }

        public int UpdateFrom7()
        {
            _cacheFileService.Empty();

            SchemaBuilder.AlterTable("CombinatorSettingsPartRecord",
                table =>
                {
                    table.DropColumn("CombineCDNResources");
                    table.AddColumn<bool>("CombineCdnResources");
                }
            );

            return 8;
        }

        public int UpdateFrom8()
        {
            SchemaBuilder.AlterTable("CombinatorSettingsPartRecord",
                table =>
                {
                    table.AddColumn<bool>("GenerateImageSprites");
                    table.AddColumn<string>("ResourceDomain");
                }
            );

            return 9;
        }

        public int UpdateFrom9()
        {
            // Changing cache file folder
            _cacheFileService.Empty();


            return 10;
        }

        public int UpdateFrom10()
        {
            // Cache files are now in a hidden folder in Media
            _cacheFileService.Empty();


            return 11;
        }

        // Swapping the HashCode column with Fingerprint happens in two steps to avoid backwards-incompatible schema changes in one version.
        public int UpdateFrom11()
        {
            SchemaBuilder.AlterTable(typeof(CombinedFileRecord).Name,
                table =>
                {
                    table.AddColumn<string>("Fingerprint", column => column.NotNull().WithLength(1024).WithDefault(string.Empty));
                    table.CreateIndex("FileFingerprint", new[] { "Fingerprint" });
                });


            return 12;
        }

        public int UpdateFrom12()
        {
            SchemaBuilder.AlterTable(typeof(CombinedFileRecord).Name,
                table => table.DropIndex("File"));

            SchemaBuilder.AlterTable(typeof(CombinedFileRecord).Name,
                table => table.DropColumn("HashCode"));


            return 13;
        }


        public void Uninstall()
        {
            _cacheFileService.Empty();
        }
    }
}