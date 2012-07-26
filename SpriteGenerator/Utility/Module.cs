using System.Drawing;
using System;

namespace Piedone.Combinator.SpriteGenerator.Utility
{
    internal class Module : IDisposable
    {
        private int _name;
        private int _width;
        private int _height;
        private int _xCoordinate;
        private int _yCoordinate;
        private Image _image;

        /// <summary>
        /// Module class representing an image and its size including white space around the image.
        /// </summary>
        public Module(int name, Image image)
        {
            _name = name;

            if (image != null)
            {
                _width = image.Width;
                _height = image.Height;
            }
            //Empty module
            else
                _width = _height = 0;

            _xCoordinate = 0;
            _yCoordinate = 0; 
            _image = image;
        }

        /// <summary>
        /// Gets the width of the module.
        /// </summary>
        public int Width
        {
            get { return _width; }
        }

        /// <summary>
        /// Gets the height of the module.
        /// </summary>
        public int Height
        {
            get { return _height; }
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the module's bottom left corner.
        /// </summary>
        public int X
        {
            get { return _xCoordinate; }
            set { _xCoordinate = value; }
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the module's bottom left corner.
        /// </summary>
        public int Y
        {
            get { return _yCoordinate; }
            set { _yCoordinate = value; }
        }

        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public int Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Sets coordinates of module to zero.
        /// </summary>
        public void ClearCoordinates()
        {
            _xCoordinate = 0;
            _yCoordinate = 0;
        }

        /// <summary>
        /// Deep copy.
        /// </summary>
        /// <returns></returns>
        public Module Copy()
        {
            var copy = new Module(_name, _image);
            copy._xCoordinate = _xCoordinate;
            copy._yCoordinate = _yCoordinate;
            return copy;
        }

        /// <summary>
        /// Draws the module into a graphics object.
        /// </summary>
        /// <param name="graphics"></param>
        public void Draw(Graphics graphics)
        {
            graphics.DrawImage(_image, _xCoordinate, _yCoordinate, _image.Width, _image.Height);
        }

        public void Dispose()
        {
            _image.Dispose();
        }
    }
}
