using System;
using System.Drawing;
using Piedone.Combinator.Extensions;

namespace Piedone.Combinator.SpriteGenerator
{
    internal class BackgroundImage
    {
        public Uri Url { get; set; }
        public Point Position { get; set; }


        public override string ToString()
        {
            var declaration = "";

            if (Url != null) declaration += "background-image: url('" + Url.ToProtocolRelative() + "');";
            if (Position != null) declaration += "background-position:" + Position.X + "px " + Position.Y + "px;";

            return declaration;
        }
    }
}