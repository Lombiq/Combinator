using System;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Migrations
{
    [OrchardFeature("Piedone.Combinator")]
    public class Migrations : DataMigrationImpl
    {
        public int Create()
        {
            SchemaBuilder.CreateTable("CombinedFileRecord", table => table
                .Column<int>("Id", column => column.PrimaryKey().Identity())
                .Column<int>("HashCode", column => column.NotNull())
                .Column<int>("Slice")
                .Column<string>("Type")
                .Column<DateTime>("LastUpdatedUtc")
            ).AlterTable("CombinedFileRecord",
                table => table
                    .CreateIndex("File", new string[] { "HashCode" })
                );

            SchemaBuilder.CreateTable("CombinatorSettingsPartRecord", table => table
                .ContentPartRecord()
                .Column<bool>("CombineCDNResources")
            );


            return 1;
        }
    }
}