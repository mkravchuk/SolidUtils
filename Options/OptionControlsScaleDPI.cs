using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Rhino;

namespace SolidUtils
{
    public enum ControlsScaleDPIType
    {
        Small, Medium, Large,
    }

    public class OptionControlsScaleDPI : OptionEnum<ControlsScaleDPIType>
    {
        public float LargeFontSize;
        public readonly string Name;
        private readonly Control[] Controls;

        public OptionControlsScaleDPI(Type[] relatedTo, Control[] controls)
            : base( controls[0].GetType().Name, "GUI size", relatedTo, OptionType.GUI)
        {
            InitAsEnum(ControlsScaleDPIType.Large, new[] {"Small", "Medium", "Large"});
            Name = controls[0].GetType().Name;
            LargeFontSize = controls[0].Font.Size; // we are using large font size by default: new Font("Segoe UI", 9);
            //LargeFontSize = 9;
            this.Controls = controls;
            this.OnChange += OnChangeEvent;
        }

        private void OnChangeEvent(OptionBase option)
        {
            SetGUIScale();
        }

        public override bool Load()
        {
            var res = base.Load();
            SetGUIScale();
            return res;
        }

        private Font GetFont(Control c)
        {
            switch (Value)
            {
                case ControlsScaleDPIType.Large:
                    return new Font(c.Font.FontFamily, LargeFontSize, c.Font.Style);
                case ControlsScaleDPIType.Medium:
                    return new Font(c.Font.FontFamily, LargeFontSize - 1, c.Font.Style);
                case ControlsScaleDPIType.Small:
                    return new Font(c.Font.FontFamily, LargeFontSize - 2, c.Font.Style);
                default:
                    return new Font(c.Font.FontFamily, LargeFontSize, c.Font.Style);
            }
        }

        private Size GetSize()
        {
            switch (Value)
            {
                case ControlsScaleDPIType.Large:
                    return new Size(48, 48);
                case ControlsScaleDPIType.Medium:
                    return new Size(36, 36);
                case ControlsScaleDPIType.Small:
                    return new Size(24, 24);
                default:
                    return new Size(48, 48);
            }
        }

        private void SetGUIScale_Font(Control control)
        {
            var font = GetFont(control);
            control.Font = font;
        }

        private void SetGUIScale()
        {
            //log.temp("New scale for {0} is {1}", Name, Value);
            foreach (var c in Controls)
            {
                if (c is ContextMenuStrip)
                {
                    SetGUIScale_ContextMenuStrip(c as ContextMenuStrip);
                }
                else if (c is StatusStrip)
                {
                    SetGUIScale_StatusStrip(c as StatusStrip);
                }
                else if (c is ToolStrip)
                {
                    SetGUIScale_ToolStrip(c as ToolStrip);
                }
                else if (c is ObjectListView)
                {
                    SetGUIScale_ObjectListView(c as ObjectListView);
                }
                else
                {
                    SetGUIScale_Font(c);
                }
            }
        }

        private void SetGUIScale_ContextMenuStrip(ContextMenuStrip menu)
        {
            SetGUIScale_Font(menu);
            //menu.RenderMode = ToolStripRenderMode.System;
        }

        private void SetGUIScale_StatusStrip(StatusStrip statusStrip)
        {
            SetGUIScale_Font(statusStrip);
            statusStrip.Height = TextRenderer.MeasureText("Az", statusStrip.Font).Height;
        }

        private void SetGUIScale_ToolStrip(ToolStrip toolStrip)
        {
            var size = GetSize();
            toolStrip.ImageScalingSize = size;
            toolStrip.Height = size.Height;
            SetGUIScale_Font(toolStrip);
        }

        private void SetGUIScale_ObjectListView(ObjectListView objectListView)
        {
            SetGUIScale_Font(objectListView);
        }
    }
}
