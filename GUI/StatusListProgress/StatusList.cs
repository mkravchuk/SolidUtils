using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using System.Drawing.Drawing2D;
namespace SolidUtils.GUI.StatusListProgress
{

    [Designer(typeof(StatusListDesigner))]
    public partial class StatusList
    {

        #region " Declarations "

        // The StatusItem Collection
        private StatusCollection _Items;
        // The currently selected item in the designer
        private StatusItem highlightedItem;
        // the pending image
        private Image _FailedImage;
        // the complete image
        private Image _CompleteImage;
        // the image size
        private Size _imageSize = new Size(12, 12);
        // the padding between the outer bounds and the image/text
        private int _pad = 3;
        private int emptySpaceToNextItem = 2;
        private int Indent = 30;
        private bool _ShowTitle = true;

        private Color _LineColor = Color.Green;
        #endregion

        #region " Properties "

        [Category("Appearance")]
        public Color LineColor
        {
            get { return _LineColor; }
            set
            {
                _LineColor = value;
                this.DrawItems();
            }
        }

        [Category("Appearance")]
        public bool ShowTitle
        {
            get { return _ShowTitle; }
            set
            {
                _ShowTitle = value;
                this.DrawItems();
            }
        }

        [Category("Custom Properties"), Description("The padding between the outer bounds and the image/text")]
        public int Pad
        {
            get { return _pad; }
            set
            {
                _pad = value;
                this.DrawItems();
            }
        }
        [Category("Custom Properties"), Description("Distance between items")]
        public int EmptySpaceToNextItem
        {
            get { return emptySpaceToNextItem; }
            set
            {
                emptySpaceToNextItem = value;
                this.DrawItems();
            }
        }

        //[Category("Custom Properties"), Description("The default image size")]
        //public Size ImageSize
        //{
        //    get { return _imageSize; }
        //    set
        //    {
        //        if (value.Width < 9 | value.Height < 9)
        //        {
        //            Interaction.MsgBox("Minimum size is  '12, 12'");
        //            return;
        //        }

        //        _imageSize = value;
        //        this.DrawItems();
        //    }
        //}

        public override System.Drawing.Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                this.DrawItems();
            }
        }

        [Category("Custom Properties"), Description("The image used for pending items")]
        public Image FailedImage
        {
            get { return _FailedImage; }
            set
            {
                _FailedImage = value;
                this.DrawItems();
            }
        }

        [Category("Custom Properties"), Description("The image used for completed items")]
        public Image CompleteImage
        {
            get { return _CompleteImage; }
            set
            {
                _CompleteImage = value;
                this.DrawItems();
            }
        }

        [Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Data"), Description("Returns a collection of all the status items")]
        public StatusCollection Items
        {
            get { return _Items; }
            set
            {
                _Items = value;
                this.DrawItems();
            }
        }

        #endregion

        #region " Methods "

        internal void DrawItems()
        {
            // ----------------------------
            // This methods calculates the layout of all the status items, designating them certain bounds, to stop overflow
            // ----------------------------

            StatusItem item = null;
            // The current item
            int y = 0;
            // Y-Axis position of the current item
            Size itemSize = default(Size);
            // The size of the current item

            Graphics g = this.CreateGraphics();

            if (!this.ShowTitle)
            {
                y = 0;
            }
            else
            {
                y = g.MeasureString(this.Text, new Font(Font, FontStyle.Bold)).ToSize().Height + Pad * 5;
            }

            try
            {
                foreach (StatusItem item_loopVariable in Items)
                {
                    item = item_loopVariable;
                    // Measure the string size 
                    var itemFont = (item.CustomFontSize.HasValue && item.CustomFontSize.Value > 0)
                      ? new Font(Font.Name, item.CustomFontSize.Value, Font.Style)
                      : new Font(Font, FontStyle.Bold);

                    var itemSizeMeasureStringSize = g.MeasureString(item.Text, itemFont).ToSize();
                    itemSize = new Size(
                        itemSizeMeasureStringSize.Width - 4,
                        itemSizeMeasureStringSize.Height);

                    // Check if the image height is larger than the current height
                    var imageHeight = Math.Max(CompleteImage.Height, FailedImage.Height);
                    if (itemSize.Height < imageHeight)
                    {
                        itemSize.Height = imageHeight; // If it is, resize the control to accommodate it
                    }

                    // Set the bounds of the control to the width of the image, text and associated padding for nicer viewing
                    //item.Bounds = New Rectangle(Pad, y, ImageSize.Width + (Pad * 3) + itemSize.Width + (Pad * 2), (itemSize.Height + Pad * 2))
                    var padX = item.CustomPaddingX ?? Pad;
                    var padY = item.CustomPaddingY ?? Pad;
                    item.Bounds = new Rectangle(Indent, y, itemSize.Width + padX * 2, itemSize.Height + padY * 2);
                    if (string.IsNullOrEmpty(item.Text))
                    {
                        item.Bounds.Width = 10;
                    }

                    // Set the Y-Axis position of the next item
                    int dist = item.CustomEmptySpaceToNextItem ?? EmptySpaceToNextItem;
                    y = item.Bounds.Bottom + dist;
                }
            }
            catch (Exception ex)
            {
            }

            g.Dispose();

            //Mark the control as invalid so it gets redrawn
            Invalidate();
        }

        public new string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                this.DrawItems();
            }
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);

            // If the control is resized, re-calculate the items and make the control invalid
            this.DrawItems();
        }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            // ----------------------------
            // This method performs all the painting of the items, text and images.
            // ----------------------------
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (this.ShowTitle)
            {
                e.Graphics.DrawString(this.Text, new Font(Font, FontStyle.Bold), new SolidBrush(this.ForeColor), 0, 0);

                int y = e.Graphics.MeasureString(this.Text, new Font(Font, FontStyle.Bold)).ToSize().Height + Pad * 2;
                LinearGradientBrush lin = new LinearGradientBrush(new Point(Pad, 0), new Point(this.Width - Pad, 0), LineColor, Color.FromArgb(0, LineColor));
                e.Graphics.DrawLine(new Pen(lin), Pad, y, this.Width - Pad, y);
            }

            // ----------------------------------------------
            // This methods needs to be cleaned up...  Please don't judge just yet!
            // ----------------------------------------------

            StatusItem item = null;
            // The current item	
            Brush b = null;
            // The current brush
            Rectangle wrct = default(Rectangle);
            // The current item bounds
            int yOffSet = 0;
            // The offSet of each item on the Y-Axis

            foreach (StatusItem item_loopVariable in Items)
            {
                item = item_loopVariable;
                var itemFont = (item.CustomFontSize.HasValue && item.CustomFontSize.Value > 0)
                      ? new Font(Font.Name, item.CustomFontSize.Value, Font.Style)
                      : new Font(Font, Font.Style);
                //Create brush from button colour
                if ((b != null))
                    b.Dispose();
                b = new SolidBrush(Color.FromArgb(180, Color.SteelBlue));

                //Fill rectangle with this colour
                wrct = item.Bounds;
                wrct.Width = this.Width - item.Bounds.Left - 30; // minus position, minus icon size

                if (string.IsNullOrEmpty(item.Text) & this.DesignMode)
                {
                    Pen p = new Pen(Color.Black);
                    p.DashStyle = DashStyle.Dot;
                    e.Graphics.DrawRectangle(p, wrct);
                }
                
                switch (item.Status)
                {
                    case StatusItem.CurrentStatus.Complete:
                        e.Graphics.DrawImage(this.CompleteImage, new Rectangle(wrct.Left/3, wrct.Top + (wrct.Height - CompleteImage.Height) / 2, CompleteImage.Width, CompleteImage.Height));
                        break;
                    case StatusItem.CurrentStatus.Failed:
                        e.Graphics.DrawImage(this.FailedImage, new Rectangle(wrct.Left / 3, wrct.Top + (wrct.Height - FailedImage.Height) / 2, FailedImage.Width, FailedImage.Height));
                        break;
                    case StatusItem.CurrentStatus.Pending:
                        e.Graphics.DrawString(item.Text, itemFont, new SolidBrush(this.ForeColor), wrct.Left + Pad, wrct.Top + Pad);
                        break;
                    case StatusItem.CurrentStatus.Running:
                        double wid = 0;
                        double range = 0;
                        range = item.Maximum - item.Minimum;
                        wid = (item.Maximum - (item.Maximum - item.Value)) / range;

                        var alpha = 100;
                        wrct.Inflate(-1, 0);
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(alpha, 227, 247, 255)), wrct.Left, wrct.Top, Convert.ToInt32(wrct.Width * wid), wrct.Height);
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(alpha, 185, 233, 252)), wrct.Left, wrct.Bottom - Convert.ToInt32(wrct.Height * 0.55), Convert.ToInt32(wrct.Width * wid), wrct.Height - Convert.ToInt32(wrct.Height * 0.55) + 1);
                        wrct.Inflate(1, 0);

                        e.Graphics.DrawLine(new Pen(Color.FromArgb(alpha, 147, 201, 227)), wrct.Left + 1, wrct.Top, wrct.Right - 1, wrct.Top);
                        e.Graphics.DrawLine(new Pen(Color.FromArgb(alpha, 147, 201, 227)), wrct.Left + 1, wrct.Bottom, wrct.Right - 1, wrct.Bottom);
                        e.Graphics.DrawLine(new Pen(Color.FromArgb(alpha, 136, 203, 235)), wrct.Left, wrct.Top + 1, wrct.Left, wrct.Bottom - 1);
                        e.Graphics.DrawLine(new Pen(Color.FromArgb(alpha, 105, 187, 227)), wrct.Left, wrct.Bottom - Convert.ToInt32(wrct.Height * 0.55), wrct.Left, wrct.Bottom - 1);
                        e.Graphics.DrawLine(new Pen(Color.FromArgb(alpha, 136, 203, 235)), wrct.Right, wrct.Top + 1, wrct.Right, wrct.Bottom - 1);
                        e.Graphics.DrawLine(new Pen(Color.FromArgb(alpha, 105, 187, 227)), wrct.Right, wrct.Bottom - Convert.ToInt32(wrct.Height * 0.55), wrct.Right, wrct.Bottom - 1);

                        var percent = Convert.ToInt32(wid * 100);
                        float progressFontSize = 5;
                        if (item.CustomFontSize.HasValue && item.CustomFontSize.Value > 0) progressFontSize = item.CustomFontSize.Value;
                        var progressFont = new Font(Font.Name, progressFontSize, FontStyle.Regular);
                        int progressTextHeight = e.Graphics.MeasureString(percent + "%", progressFont).ToSize().Height;
                        e.Graphics.DrawString(percent + "%", progressFont, new SolidBrush(Color.FromArgb(alpha, 147, 201, 227)), 0, wrct.Bottom - Convert.ToInt32(wrct.Height / 2) - progressTextHeight / 2);
                        break;
                }

                int itemTextHeight = e.Graphics.MeasureString(item.Text, itemFont).ToSize().Height;
                var padX = item.CustomPaddingX ?? Pad;
                e.Graphics.DrawString(item.Text, itemFont, new SolidBrush(Color.FromArgb(220, this.ForeColor)), wrct.Left + padX, wrct.Bottom - Convert.ToInt32(wrct.Height / 2) - itemTextHeight / 2);

                if (object.ReferenceEquals(highlightedItem, item))
                {
                    wrct.Inflate(-1, -1);
                    e.Graphics.DrawRectangle(new Pen(b, 2), wrct);
                }
            }
        }

        internal void OnSelectionChanged()
        {
            // ----------------------------
            // This methods is called by the StatusLabel control when a selection has changed in design mode
            // ----------------------------

            StatusItem item = null;
            StatusItem newHighlightedItem = null;
            ISelectionService s = (ISelectionService)GetService(typeof(ISelectionService));

            //See if the primary selection is one of our buttons
            foreach (StatusItem item_loopVariable in Items)
            {
                item = item_loopVariable;
                if (object.ReferenceEquals(s.PrimarySelection, item))
                {
                    newHighlightedItem = item;
                    break; // TODO: might not be correct. Was : Exit For
                }
            }

            //Apply if necessary
            if ((!object.ReferenceEquals(newHighlightedItem, highlightedItem)))
            {
                highlightedItem = newHighlightedItem;
                Invalidate();
            }
        }

        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            // ----------------------------
            // This methods checks the MouseDown event within the control and determines whether an item was selected, and if so, which one
            // ----------------------------

            Rectangle wrct = default(Rectangle);
            // The current item bounds
            ISelectionService s = null;
            // Selection service
            ArrayList a = null;
            // array of selected items
            StatusItem item = null;
            // the current item

            if (DesignMode)
            {
                foreach (StatusItem item_loopVariable in Items)
                {
                    item = item_loopVariable;
                    // Get the current item bounds
                    wrct = item.Bounds;
                    if (wrct.Contains(e.X, e.Y))
                    {
                        // if the current item has mouse down focus, add it to the array and exit the loop
                        s = (ISelectionService)GetService(typeof(ISelectionService));
                        a = new ArrayList();
                        a.Add(item);
                        s.SetSelectedComponents(a);
                        break; // TODO: might not be correct. Was : Exit For
                    }
                }
            }

            // Perform any additional tasks here
            base.OnMouseDown(e);
        }

        #endregion

        public StatusList()
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();

            // Initialisations go here...
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            // IMPORTANT!!! This declares the New Collection
            _Items = new StatusCollection(this);

            // Set the default size of the control
            this.Size = new Size(200, 100);
        }
    }
}
