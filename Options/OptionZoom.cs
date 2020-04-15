using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolidUtils
{
    public enum OptionZoomStyle
    {
        Zoom1, Zoom2, Zoom3, Zoom5, Zoom10, Zoom15, Zoom30, Zoom60
    }

    public class OptionZoom
    {
        public OptionBool Zoom { get; set; }
        public OptionEnum<OptionZoomStyle> ZoomStyle { get; set; }

        public OptionZoom(string key, bool zoom, OptionZoomStyle style, string caption, Type[] relatedTo)
        {
            Zoom = new OptionBool( key + "_zoom", zoom, caption, relatedTo, OptionType.Zoom);
            ZoomStyle = new OptionEnum<OptionZoomStyle>(key + "_zoomstyle", "Zoom", relatedTo, OptionType.Zoom)
                .InitAsEnum(style, new[] { "100%", "50%", "30%", "20%", "10%", "6%", "3%", "2%" });
            Zoom.AddChilds(ZoomStyle);
        }

        public double Level
        {
            get
            {
                var zoom = ZoomStyle.Value;
                if (!Zoom)
                {
                    return OptionZoomSkippValueOvverider.MinLevel;
                }
                return OptionZoomEnum_TO_Num(zoom);
            }
        }

        public static double OptionZoomEnum_TO_Num(OptionZoomStyle zoomStyle)
        {
            switch (zoomStyle)
            {
                case OptionZoomStyle.Zoom1:
                    return -0.1;
                case OptionZoomStyle.Zoom2:
                    return 2;
                case OptionZoomStyle.Zoom3:
                    return 3;
                case OptionZoomStyle.Zoom5:
                    return 5;
                case OptionZoomStyle.Zoom10:
                    return 10;
                case OptionZoomStyle.Zoom15:
                    return 15;
                case OptionZoomStyle.Zoom30:
                    return 30;
                case OptionZoomStyle.Zoom60:
                    return 60;
                default:
                    return 0;
            }
        }

        public class OptionZoomSkippValueOvverider : IDisposable
        {
            public static double MinLevel = 0;

            public OptionZoomSkippValueOvverider(OptionZoomStyle min)
            {
                MinLevel = OptionZoomEnum_TO_Num(min);
            }

            public void Dispose()
            {
                MinLevel = 0;
            }
        }
    }
}
