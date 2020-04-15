using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.Input.Custom;
using SolidUtils.DisplayModes;

namespace SolidUtils
{
    public static partial class Layers
    {
        /// <summary>
        /// Only for debuging. 
        /// Will work only when Visual Studio Debugger is attached.
        /// </summary>
        private static readonly _DebugLayer _debug;
        private static readonly _HighlightLayer _highlightLayer;

        public static _DebugLayer Debug
        {
            get
            {
                Viewport.RedrawWhenPossiblePlease();
                return _debug;
            }
        }

        public static _HighlightLayer HighlightLayer
        {
            get
            {
                Viewport.RedrawWhenPossiblePlease();
                return _highlightLayer;
            }
        }

        static Layers()
        {
            _debug = new _DebugLayer();
            _highlightLayer = new _HighlightLayer();
        }
    }

    public class _HighlightLayer : BaseLayer
    {
        public override int LAYER_INDEX
        {
            get { return Layers.LayerIndexes.HighlighLayerIndex; }
        }

        public _HighlightLayer()
            : base(Layers.LayerIndexes.LAYER_NAME_HighlightLayer)
        {
        }
    }

    public class _DebugLayer : BaseLayer
    {
        public override int LAYER_INDEX
        {
            get { return Layers.LayerIndexes.DebugLayerIndex; }
        }
        
        public override bool IS_ENABLED
        {
            get
            {
                return Shared.IsDebugMode && base.IS_ENABLED;
            }
        }

        public _DebugLayer()
            : base(Layers.LayerIndexes.LAYER_NAME_DebugLayer)
        {
        }
    }

    
}
