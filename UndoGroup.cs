using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;

namespace SolidUtils
{
    public class UndoGroup : IDisposable
    {
        private const bool DEBUG = false;

        public string Text { get; set; }
        public uint UndoID { get; set; }
        public RhinoDoc Doc { get; set; }

        public UndoGroup(RhinoDoc doc, string text)
        {
            Text = text;
            Doc = doc;
            if (DEBUG)
            {
                log.debug(g.None, "UNDO STARTED...");
                log.IndentLevel++;
            }
            UndoID = doc.BeginUndoRecord(text);
            //log.debug(g.None, text);
        }

        public void Dispose()
        {
            Doc.EndUndoRecord(UndoID);
            if (DEBUG)
            {
                log.IndentLevel--;
                log.debug(g.None, "UNDO ENDED.");
            }
        }
    }
}
