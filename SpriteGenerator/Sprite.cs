using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Piedone.Combinator.SpriteGenerator.Utility;

namespace Piedone.Combinator.SpriteGenerator
{
    /// <remarks>
    /// The original version of this code is from the SpriteGenerator tool (http://spritegenerator.codeplex.com) by Csilla Karaffa
    /// </remarks>
    internal class Sprite : IDisposable
    {
        private readonly IEnumerable<byte[]> _imageContents;
        private Placement _placement;
        private List<Module> _modules;

        public Sprite(IEnumerable<byte[]> imageContents)
        {
            _imageContents = imageContents;
        }

        public IEnumerable<BackgroundImage> Generate(Stream sprite, ImageFormat spriteFormat)
        {
            CreateModules();

            if (_modules.Count == 0) return Enumerable.Empty<BackgroundImage>();

            GenerateLayout(sprite, spriteFormat);
            var backgroundImages = GenerateBackgroundImages();
            if (backgroundImages.Count() != _imageContents.Count())
            {
                throw new ApplicationException("Not every image was placed in the sprite. This really shouldn't happen.");
            }
            return backgroundImages;
        }

        private void CreateModules()
        {
            _modules = new List<Module>();

            var i = 0;
            foreach (var imageContent in _imageContents)
            {
                using (var ms = new MemoryStream(imageContent))
                {
                    _modules.Add(new Module(i, Image.FromStream(ms)));
                }

                i++;
            }
        }

        private void GenerateLayout(Stream sprite, ImageFormat spriteFormat)
        {
            _placement = Algorithm.Greedy(_modules);

            using (Image spriteImage = new Bitmap(_placement.Width, _placement.Height))
            {
                using (var graphics = Graphics.FromImage(spriteImage))
                {
                    foreach (var module in _placement.Modules)
                    {
                        module.Draw(graphics);
                    }
                }

                spriteImage.Save(sprite, spriteFormat);
            }
        }

        private IEnumerable<BackgroundImage> GenerateBackgroundImages()
        {
            var images = new List<BackgroundImage>();

            // We're using _modules, not _placement.Modules to have the original order of images
            foreach (var module in _modules)
            {
                images.Add(
                    new BackgroundImage
                    {
                        Position = new Point(module.X * -1, module.Y * -1)
                    });
            }

            return images;
        }

        public void Dispose()
        {
            foreach (var module in _modules)
            {
                module.Dispose();
            }
        }
    }
}
