using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;

namespace Piedone.Combinator.SpriteGenerator
{
    internal class BackgroundImage
    {
        public string ImageUrl { get; set; }
        public Point Position { get; set; }

        public override string ToString()
        {
            var declaration = "";

            if (!String.IsNullOrEmpty(ImageUrl)) declaration += "background-image: url('" + ImageUrl + "');";
            if (Position != null) declaration += "background-position:" + Position.X + "px " + Position.Y + "px;";

            return declaration;
        }
    }
}