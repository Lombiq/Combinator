using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Commands;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Services;

namespace Piedone.Combinator.Commands
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorCommands : DefaultOrchardCommandHandler
    {
        private readonly ICacheFileService _cacheFileService;


        public CombinatorCommands(ICacheFileService cacheFileService)
        {
            _cacheFileService = cacheFileService;
        }
	
			
        [CommandName("combinator empty")]
        [CommandHelp("combinator empty\r\n\t" + "Empties the Combinator cache.")]
        public void EmptyCache()
        {
            _cacheFileService.Empty();

            Context.Output.WriteLine(T("Combinator cache successfully emptied."));
        }
    }
}