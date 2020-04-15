using System;
using Rhino;

namespace SolidUtils
{
    public static class Logger
    {
        public static int IndentLevel { get; set; }
        public static bool ENABLED = true;

        public static string IndentLevelPrefix
        {
            get
            {
                string prefix = "";
                for (int i = 0; i < IndentLevel; i++)
                {
                    prefix += "     ";
                }
                return prefix;
            }
        }

        public static void log(string message, params object[] args)
        {
            if (!ENABLED) return;
            message = String.Format(message, args);
            RhinoApp.WriteLine(IndentLevelPrefix + message);
        }

        public static void log_NoEnter(string s)
        {
            if (!ENABLED) return;
            RhinoApp.Write(s);
        }

        public static void msg(string text, string title)
        {
            if (!ENABLED) return;

            log("");
            log("--------------------------------------");
            log("---" + title + "---");
            log("");
            log(text);
            log("");
            log("--------------------------------------");
            log("");

            //
            // #1
            //
            //MessageBox.Show(text);

            //
            // #2
            //
            //doesnt work with big test - edit box appears empty!!! - i dont know why.

            //text = "ON_Brep:\nsurfaces:  1\n3d curve:  3\n2d curves: 6\nvertices:  5\nedges:     5\ntrims:     6\nloops:     1\nfaces:     1\ncurve2d[ 0]: TL_NurbsCurve domain(0,0.699715) start(0.0128737,0) end(0.954987,0)\ncurve2d[ 1]: TL_NurbsCurve domain(0,1) start(1,0.5) end(1,0.5)\ncurve2d[ 2]: TL_NurbsCurve domain(0.0872745,0.154707) start(1,0.5) end(0.954545,1)\ncurve2d[ 3]: TL_NurbsCurve domain(-0.780843,0) start(0,1) end(0.0128737,0)\ncurve2d[ 4]: TL_NurbsCurve domain(0.699715,0.733034) start(0.954987,0) end(1,0.5)\ncurve2d[ 5]: TL_NurbsCurve domain(0.154707,1.5708) start(0.954545,1) end(0,1)\ncurve3d[ 0]: TL_NurbsCurve domain(0,0.733034) start(435.325,-160.161,325.005) end(435.822,-160.161,325.453)\ncurve3d[ 1]: TL_NurbsCurve domain(0.0872745,1.5708) start(435.822,-160.161,325.453) end(435.822,-160.659,324.997)\ncurve3d[ 2]: TL_NurbsCurve domain(-0.780843,0) start(435.822,-160.659,324.997) end(435.325,-160.161,325.005)\nsurface[ 0]: TL_NurbsSurface u(0,1) v(0,1)\nvertex[ 0]: (435.324917 -160.161210 325.005109) tolerance(8.64613e-08)\n\tedges (0,2)\nvertex[ 1]: (435.791433 -160.161033 325.451895) tolerance(1.17002e-08)\n\tedges (0,3)\nvertex[ 2]: (435.822178 -160.161032 325.452841) tolerance(0)\n\tedges (1,3)\nvertex[ 3]: (435.822178 -160.192082 325.451888) tolerance(7.53934e-08)\n\tedges (1,4)\nvertex[ 4]: (435.822178 -160.659311 324.996618) tolerance(2.51321e-06)\n\tedges (2,4)\nedge[ 0]: v0( 0) v1( 1) 3d_curve(0) tolerance(8.63749e-08)\n\tdomain(0,0.699715) start(435.325,-160.161,325.005) end(435.791,-160.161,325.452)\n\ttrims (+0)\nedge[ 1]: v0( 2) v1( 3) 3d_curve(1) tolerance(0.00249911)\n\tdomain(0.0872745,0.154707) start(435.822,-160.161,325.453) end(435.822,-160.192,325.452)\n\ttrims (+3)\nedge[ 2]: v0( 4) v1( 0) 3d_curve(2) tolerance(2.5107e-06)\n\tdomain(-0.780843,0) start(435.822,-160.659,324.997) end(435.325,-160.161,325.005)\n\ttrims (+5)\nedge[ 3]: v0( 1) v1( 2) 3d_curve(0) tolerance(0.00247504)\n\tdomain(0.699715,0.733034) start(435.791,-160.161,325.452) end(435.822,-160.161,325.453)\n\ttrims (+1)\nedge[ 4]: v0( 3) v1( 4) 3d_curve(1) tolerance(2.5107e-06)\n\tdomain(0.154707,1.5708) start(435.822,-160.192,325.452) end(435.822,-160.659,324.997)\n\ttrims (+4)\nface[ 0]: surface(0) reverse(0) loops(0)\n\tFast render mesh: 0 polygons\n\tloop[ 0]: type(outer) 6 trims(0,1,2,3,4,5)\n\t\ttrim[ 0]: edge( 0) v0( 0) v1( 1) tolerance(0,1.54767e-07)\n\t\t\ttype(boundary-south side iso) rev3d(0) 2d_curve(0)\n\t\t\tdomain(0,0.699715) start(0.0128737,0) end(0.954987,0)\n\t\t\tsurface points start(435.325,-160.161,325.005) end(435.791,-160.161,325.452)\n\t\ttrim[ 1]: edge( 3) v0( 1) v1( 2) tolerance(0,0.334961)\n\t\t\ttype(boundary) rev3d(0) 2d_curve(4)\n\t\t\tdomain(0.699715,0.733034) start(0.954987,0) end(1,0.5)\n\t\t\tsurface points start(435.791,-160.161,325.452) end(435.822,-160.161,325.453)\n\t\ttrim[ 2]: edge(-1) v0( 2) v1( 2) tolerance(0,0)\n\t\t\ttype(singular) rev3d(0) 2d_curve(1)\n\t\t\tdomain(0,1) start(1,0.5) end(1,0.5)\n\t\t\tsurface points start(435.822,-160.161,325.453) end(435.822,-160.161,325.453)\n\t\ttrim[ 3]: edge( 1) v0( 2) v1( 3) tolerance(0,0.334961)\n\t\t\ttype(boundary) rev3d(0) 2d_curve(2)\n\t\t\tdomain(0.0872745,0.154707) start(1,0.5) end(0.954545,1)\n\t\t\tsurface points start(435.822,-160.161,325.453) end(435.822,-160.192,325.452)\n\t\ttrim[ 4]: edge( 4) v0( 3) v1( 4) tolerance(0,0)\n\t\t\ttype(boundary-north side iso) rev3d(0) 2d_curve(5)\n\t\t\tdomain(0.154707,1.5708) start(0.954545,1) end(0,1)\n\t\t\tsurface points start(435.822,-160.192,325.452) end(435.822,-160.659,324.997)\n\t\ttrim[ 5]: edge( 2) v0( 4) v1( 0) tolerance(6.83277e-08,3.10794e-09)\n\t\t\ttype(boundary) rev3d(0) 2d_curve(3)\n\t\t\tdomain(-0.780843,0) start(0,1) end(0.0128737,0)\n\t\t\tsurface points start(435.822,-160.659,324.997) end(435.325,-160.161,325.005)";
            //Rhino.UI.Dialogs.ShowTextDialog(text, title);


            //
            // #3
            //
            //string outtext;
            //Rhino.UI.Dialogs.ShowEditBox(title, text, "ee", true, out outtext);
        }

        public static void logError(Exception ex, string errorDescription = "")
        {
            if (errorDescription != "")
            {
                errorDescription = " " + errorDescription;
            }
            log("ERROR{0}:  {1}", errorDescription, ex.Message);
            //HostUtils.ExceptionReport(ex);
        }


    }
}
