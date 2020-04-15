using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Rhino.Display;

namespace SolidUtils.Conduit
{
    public class ConduitTextDownRightCorver : DisplayConduit
    {
        public string Text { get; set; }
        public Color Color { get; set; }

        public ConduitTextDownRightCorver(string text, Color color)
        {
            Text = text;
            Color = color;
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            var bounds = e.Viewport.Bounds;
            var pt = new Rhino.Geometry.Point2d(bounds.Right - 100, bounds.Bottom - 30);
            e.Display.Draw2dText(Text, Color, pt, false);
        }
    }
}
