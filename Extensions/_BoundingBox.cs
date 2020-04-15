using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace SolidUtils
{
    public static class _BoundingBox
    {
        public static double _Diagonal(this BoundingBox box)
        {
            return box.Max._DistanceTo(box.Min);
        }

        public static double _Width(this BoundingBox box)
        {
            return box.Max.X - box.Min.X;
        }

        public static double _Height(this BoundingBox box)
        {
            return box.Max.Y - box.Min.Y;
        }

        public static double _Deep(this BoundingBox box)
        {
            return box.Max.Z - box.Min.Z;
        }

        public static Percent _GetSizeDiffInPercent(this BoundingBox box, BoundingBox compareToBox, Percent minDiffInPercentAllowed)
        {
            Percent widthDiff = box._Width()._DiffInPercent(compareToBox._Width());
            if (widthDiff < minDiffInPercentAllowed) return widthDiff; // speed optimization
            Percent heightDiff = box._Height()._DiffInPercent(compareToBox._Height());
            if (heightDiff < minDiffInPercentAllowed) return heightDiff; // speed optimization

            //var boxCenter = box.Center;
            //var compareToBoxCenter = compareToBox.Center;
            //Percent Xdiff = 1- Math.Abs(boxCenter.X - compareToBoxCenter.X)._DiffInPercent(Math.Min(box._Width(), compareToBox._Width()));
            //if (widthDiff < minDiffInPercentAllowed) return widthDiff; // speed optimization
            //Percent Ydiff = 1 - Math.Abs(boxCenter.Y - compareToBoxCenter.Y)._DiffInPercent(Math.Min(box._Height(), compareToBox._Height()));
            //if (Ydiff < minDiffInPercentAllowed) return Ydiff; // speed optimization
            //Percent Zdiff = 1 - Math.Abs(boxCenter.Z - compareToBoxCenter.Z)._DiffInPercent(Math.Min(box._Deep(), compareToBox._Deep()));

            Percent diagonalDiff = box._Diagonal()._DiffInPercent(compareToBox._Diagonal());
            Percent deepDiff = box._Deep()._DiffInPercent(compareToBox._Deep());



            double minPercent = 1;
            foreach (var diff in new[] { diagonalDiff, widthDiff, heightDiff, deepDiff, }) // Xdiff, Ydiff, Zdiff
            {
                if (diff < minPercent)
                {
                    minPercent = diff;
                }
            }

            return new Percent(minPercent);
        }

        public static BoundingBox _UnionFast(this BoundingBox a, BoundingBox b)
        {
            // ver 1 - slow - does a lot of validations
            //return BoundingBox.Union(a, b); 

            // ver 2 - fast
            return new BoundingBox()
            {
                Min = new Point3d
                {
                    X = a.Min.X < b.Min.X ? a.Min.X : b.Min.X,
                    Y = a.Min.Y < b.Min.Y ? a.Min.Y : b.Min.Y,
                    Z = a.Min.Z < b.Min.Z ? a.Min.Z : b.Min.Z,
                },
                Max = new Point3d
                {
                    X = a.Max.X > b.Max.X ? a.Max.X : b.Max.X,
                    Y = a.Max.Y > b.Max.Y ? a.Max.Y : b.Max.Y,
                    Z = a.Max.Z > b.Max.Z ? a.Max.Z : b.Max.Z,
                }
            };
        }



        public static Percent _GetPositionDiffInPercent(this BoundingBox box, BoundingBox compareToBox, Percent minDiffInPercentAllowed)
        {
            return box._GetSizeDiffInPercent( box._UnionFast(compareToBox), minDiffInPercentAllowed);
        }
    }
}
