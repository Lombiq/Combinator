using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SpriteGenerator.Utility;

namespace SpriteGenerator {
    class Sprite {
        private Dictionary<int, Image> _images;
        private Dictionary<int, string> _cssClassNames;
        private LayoutProperties _layoutProperties;

        public Sprite(LayoutProperties layoutProperties) {
            _images = new Dictionary<int, Image>();
            _cssClassNames = new Dictionary<int, string>();
            _layoutProperties = layoutProperties;
        }

        public void Create() {
            GetData(out _images, out _cssClassNames);

            StreamWriter cssFile = File.CreateText(_layoutProperties.outputCssFilePath);
            Image resultSprite = null;

            resultSprite = GenerateLayout(cssFile);

            cssFile.Close();
            FileStream outputSpriteFile = new FileStream(_layoutProperties.outputSpriteFilePath, FileMode.Create);
            resultSprite.Save(outputSpriteFile, ImageFormat.Png);
            outputSpriteFile.Close();
        }

        /// <summary>
        /// Creates dictionary of images from the given paths and dictionary of CSS classnames from the image filenames.
        /// </summary> 
        /// <param name="inputFilePaths">Array of input file paths.</param>
        /// <param name="images">Dictionary of images to be inserted into the output sprite.</param>
        /// <param name="cssClassNames">Dictionary of CSS classnames.</param>
        private void GetData(out Dictionary<int, Image> images, out Dictionary<int, string> cssClassNames) {
            images = new Dictionary<int, Image>();
            cssClassNames = new Dictionary<int, string>();

            for (int i = 0; i < _layoutProperties.inputFilePaths.Length; i++) {
                Image img = Image.FromFile(_layoutProperties.inputFilePaths[i]);
                images.Add(i, img);
                string[] splittedFilePath = _layoutProperties.inputFilePaths[i].Split('\\');
                cssClassNames.Add(i, splittedFilePath[splittedFilePath.Length - 1].Split('.')[0]);
            }
        }

        private List<Module> CreateModules() {
            var modules = new List<Module>();
            foreach (int i in _images.Keys)
                modules.Add(new Module(i, _images[i]));
            return modules;
        }

        private string CssLine(string cssClassName, Rectangle rectangle) {
            return
                "background-image:url('');background-repeat: no-repeat;" + 
                "width:" + rectangle.Width.ToString() + 
                "px;height:" + rectangle.Height.ToString() +
                "px;background-position:" + (-1 * rectangle.X).ToString() + "px " + (-1 * rectangle.Y).ToString() + "px;";
        }

        //Automatic layout
        private Image GenerateLayout(StreamWriter cssFile) {
            var sortedByArea = from m in CreateModules()
                               orderby m.Width * m.Height descending
                               select m;
            var moduleList = sortedByArea.ToList<Module>();
            var placement = Algorithm.Greedy(moduleList);

            //Creating an empty result image.
            Image resultSprite = new Bitmap(placement.Width, placement.Height);
            var graphics = Graphics.FromImage(resultSprite);

            //Drawing images into the result image in the original order and writing CSS lines.
            foreach (Module m in placement.Modules) {
                m.Draw(graphics);
                var rectangle = new Rectangle(m.X, m.Y, m.Width, m.Height);
                cssFile.WriteLine(CssLine(_cssClassNames[m.Name], rectangle));
            }

            return resultSprite;
        }
    }
}
