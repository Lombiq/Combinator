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

        public IEnumerable<string> Generate(Stream sprite)
        {
            CreateModules();
            GenerateLayout(sprite);
            var backgroundDeclarations = GenerateBackgroundDeclarations();
            if (backgroundDeclarations.Count() != _imageContents.Count())
            {
                throw new ApplicationException("Not every image was placed in the sprite. This really shouldn1t happen.");
            }
            return backgroundDeclarations;
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

        private void GenerateLayout(Stream sprite)
        {
            _placement = Algorithm.Greedy(_modules);

            using (Image spriteImage = new Bitmap(_placement.Width, _placement.Height))
            {
                using (var graphics = Graphics.FromImage(spriteImage))
                {
                    //Drawing images into the result image in the original order and writing CSS lines.
                    foreach (var module in _placement.Modules)
                    {
                        module.Draw(graphics);
                    }
                }

                spriteImage.Save(sprite, ImageFormat.Png);
            }
        }

        private IEnumerable<string> GenerateBackgroundDeclarations()
        {
            var declarations = new List<string>();

            foreach (var module in _placement.Modules)
            {
                var rectangle = new Rectangle(module.X, module.Y, module.Width, module.Height);

                declarations.Add(
                    "background-image:url('');background-repeat: no-repeat;" +
                    "width:" + rectangle.Width.ToString() +
                    "px;height:" + rectangle.Height.ToString() +
                    "px;background-position:" + (-1 * rectangle.X).ToString() + "px " + (-1 * rectangle.Y).ToString() + "px;");
            }

            return declarations;
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
