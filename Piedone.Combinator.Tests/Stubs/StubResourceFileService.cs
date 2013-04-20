using System;
using System.Text;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;

namespace Piedone.Combinator.Tests.Stubs
{
    class StubResourceFileService : IResourceFileService
    {
        private readonly ResourceRepository _resourceRepository;


        public StubResourceFileService(ResourceRepository resourceRepository)
        {
            _resourceRepository = resourceRepository;
        }


        public void LoadResourceContent(CombinatorResource resource)
        {
        }

        public byte[] GetImageContent(Uri imageUrl, int maxSizeKB)
        {
            return Encoding.Unicode.GetBytes(imageUrl.ToString());
        }
    }
}
