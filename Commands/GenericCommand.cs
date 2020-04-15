using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Commands;
using SolidUtils;

namespace Rhino.Commands
{
    [CommandStyle(Style.ScriptRunner)]
    public abstract class GenericCommand : Command
    {
         ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return this.GetType().Name.Replace("Command", ""); }
        }


        //public static __CLASS__ Instance { get; private set; }

        //public __CLASS__()
        //{
        //    Instance = this;
        //}
        //~__CLASS__()
        //{
        //    Instance = null;
        //}


        public Result ExecuteCommand(RhinoDoc doc, string commandName, Action<RhinoDoc> action)
        {
            //using (new log.Group(g.RhinoCommand, "Executing: " + group))
            using (new Viewport.RedrawSuppressor(doc, "ExecuteCommand:" + commandName, false, false))
            {
                using (new log.Group(g.RhinoCommand, commandName, false))
                {

                    var failReason = "Failed to execute command '{0}'"._Format(commandName);
                    Shared.TryCatchAction(() => action(doc), g.RhinoCommand, failReason);
                }
            }
            return Result.Success;
        }
    }
}
