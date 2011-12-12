using System;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Migrations
{
    [OrchardFeature("Piedone.Combinator")]
    public class Migrations : DataMigrationImpl
    {
        public int Create()
        {
            SchemaBuilder.CreateTable(typeof(CombinedFileRecord).Name, 
                table => table
                    .Column<int>("Id", column => column.PrimaryKey().Identity())
                    .Column<int>("HashCode", column => column.NotNull())
                    .Column<int>("Slice")
                    .Column<string>("Type")
                    .Column<DateTime>("LastUpdatedUtc")
                    .Column<string>("Settings", column => column.Unlimited())
            ).AlterTable("CombinedFileRecord",
                table => table
                    .CreateIndex("File", new string[] { "HashCode" })
            );

            SchemaBuilder.CreateTable(typeof(CombinatorSettingsPartRecord).Name, 
                table => table
                    .ContentPartRecord()
                    .Column<string>("CombinationExcludeRegex")
                    .Column<bool>("CombineCDNResources")
                    .Column<bool>("MinifyResources")
                    .Column<string>("MinificationExcludeRegex")
                    .Column<bool>("EmbedCssImages")
                    .Column<int>("EmbeddedImagesMaxSizeKB")
                    .Column<string>("EmbedCssImagesExcludeRegex")
                    .Column<string>("EmbedCssImagesStylesheetExcludeRegex")
            );


            return 3;
        }

        public int UpdateFrom1()
        {
            SchemaBuilder.AlterTable(typeof(CombinatorSettingsPartRecord).Name, 
                table => table
                    .AddColumn<bool>("MinifyResources")
            );

            SchemaBuilder.AlterTable(typeof(CombinatorSettingsPartRecord).Name, 
                table => table
                    .AddColumn<string>("MinificationExcludeRegex")
            );

            return 2;
        }

        public int UpdateFrom2()
        {
            SchemaBuilder.AlterTable(typeof(CombinatorSettingsPartRecord).Name,
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
            SchemaBuilder.AlterTable(typeof(CombinatorSettingsPartRecord).Name,
                table => table
                    .AddColumn<bool>("EmbedCssImages")
            );

            SchemaBuilder.AlterTable(typeof(CombinatorSettingsPartRecord).Name,
                table => table
                    .AddColumn<int>("EmbeddedImagesMaxSizeKB")
            );

            SchemaBuilder.AlterTable(typeof(CombinatorSettingsPartRecord).Name,
                table => table
                    .AddColumn<string>("EmbedCssImagesStylesheetExcludeRegex")
            );

            return 4;
        }
    }
}