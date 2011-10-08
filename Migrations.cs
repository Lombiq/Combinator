using System;
using System.Collections.Generic;
using System.Data;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Helpers;

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
            );
            //).AlterTable("CombinedFileRecord",
            //    table => table
            //        .CreateIndex("File", new string[] { "Key", "Slice" })
            //    );


            return 1;
        }
    }
}