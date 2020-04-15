using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SolidUtils.GUI
{
    public class LabelTransparent : Label
    {
        public SolidBrush BackColorBrush;
        public SolidBrush ForeColorBrush;

        public LabelTransparent()
        {
            DoubleBuffered = true;
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            using (Brush brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
                e.Graphics.DrawString(Text, Font, ForeColorBrush, ClientRectangle);
            }
            //base.OnPaint(e);
        }
        //protected override void OnTextChanged(EventArgs e)
        //{
        //    base.OnTextChanged(e);
        //    UpdateVisual(true);
        //}
        //protected override void OnFontChanged(EventArgs e)
        //{
        //    base.OnFontChanged(e);
        //    UpdateVisual(true);
        //}
        //protected override void OnSizeChanged(EventArgs e)
        //{
        //    base.OnSizeChanged(e);
        //    UpdateVisual(true);
        //}
        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            //base.BackColor = Color.Transparent;
            ForeColorBrush = new SolidBrush(ForeColor);
        }
    }
}
