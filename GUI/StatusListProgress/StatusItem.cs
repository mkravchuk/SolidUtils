using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
namespace SolidUtils.GUI.StatusListProgress
{

	[TypeConverter(typeof(StatusItemConverter)), DesignTimeVisible(false), ToolboxItem(false)]
	public partial class StatusItem
	{

		#region " Enumerations "

		public enum CurrentStatus
		{
			Failed,
			// The failed image will be drawn
			Pending,
			// The pending image will be drawn
			Complete,
			// The complete image will be drawn
			Running
			// The pending image will be drawn
		}

		#endregion

		#region " Declarations "

			// default text for the item
		private string _text = "Status message goes here";
			// default status for the item
		private CurrentStatus _status = CurrentStatus.Pending;
		private int _Minimum = 0;
		private int _Maximum = 100;

        private int _Value = 0;
        private float? _CustomFontSize = null;
        private int? _CustomPaddingX = null;
        private int? _CustomPaddingY = null;
        private int? _CustomEmptySpaceToNextItem = null;
			// The control that this item belongs to
		internal StatusList Parent = null;
			// The bounds of the item
		internal Rectangle Bounds;

		#endregion

		#region " Properties "

		[Category("Behavior")]
		public int Minimum {
			get { return _Minimum; }
			set {
				if (!(value > this.Maximum)) {
					_Minimum = value;
				}
			}
		}

		[Category("Behavior")]
		public int Maximum {
			get { return _Maximum; }
			set {
				if (!(value < this.Minimum)) {
					_Maximum = value;
				}
			}
		}

		[Category("Behavior")]
		public int Value {
			get { return _Value; }
			set {
				if (!(value < this.Minimum) & !(value > this.Maximum)) {
					_Value = value;
					Parent.DrawItems();
				}
			}
		}

		[Category("Custom Properties"), Description("The status of the item")]
		public CurrentStatus Status {
			get { return _status; }
			set {
				_status = value;
				Parent.DrawItems();
			}
		}

        [Category("Custom Properties"), Description("The text associated with the item")]
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                Parent.DrawItems();
            }
        }

        [Category("Custom Properties"), DefaultValue(null), Description("Custom font size for item. Empty value means use default Font size  'Font.Size'")]
        public float? CustomFontSize
        {
            get { return _CustomFontSize; }
            set
            {
                _CustomFontSize = value;
                Parent.DrawItems();
            }
        }
        [Category("Custom Properties"), DefaultValue(null), Description("Custom X padding for item. Empty value  means use default padding 'Pad'")]
        public int? CustomPaddingX
        {
            get { return _CustomPaddingX; }
            set
            {
                _CustomPaddingX = value;
                Parent.DrawItems();
            }
        }
        [Category("Custom Properties"), DefaultValue(null), Description("Custom Y padding for item. Empty value  means use default padding 'Pad'")]
        public int? CustomPaddingY
        {
            get { return _CustomPaddingY; }
            set
            {
                _CustomPaddingY = value;
                Parent.DrawItems();
            }
        }
        [Category("Custom Properties"), DefaultValue(null), Description("Custom distance between items. Empty value  means use default distance 'DistBettweenItems'")]
        public int? CustomEmptySpaceToNextItem
        {
            get { return _CustomEmptySpaceToNextItem; }
            set
            {
                _CustomEmptySpaceToNextItem = value;
                Parent.DrawItems();
            }
        }



        #endregion

		[System.Diagnostics.DebuggerNonUserCode()]
		public StatusItem()
		{
			// Declare a new StatusLabel control for this item
			Parent = new StatusList();
		}

	}
}
