using System;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;
using System.Text;

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

        public string GetImageBase64Data(Uri imageUrl, int maxSizeKB)
        {
            return "base64: " + imageUrl;
        }


        public byte[] GetImageContent(Uri imageUrl, int maxSizeKB)
        {
            return Encoding.Unicode.GetBytes(imageUrl.ToString());
        }
    }
}
