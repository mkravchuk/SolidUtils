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
using Rhino.Input.Custom;
using SolidUtils.DisplayModes;


namespace SolidUtils
{
    public class BaseLayer
    {
        #region Constructor and  virtual properties

        public readonly string LAYER_NAME;

        public virtual int LAYER_INDEX
        {
            get { return -1; }
        }

        public RhinoDoc Doc
        {
            get { return RhinoDoc.ActiveDoc; }
        }

        public virtual bool IS_ENABLED
        {
            get
            {
                if (!Shared.IsExecutingInMainThread)
                {
                    log.wrong("Attempt to add object in '{0}' layer from non main thread! Try to disable 'Use multithreading' option.", LAYER_NAME);
                    return false;
                }
                if (Doc == null)
                {
                    return false;
                }
                return true;
            }
        }

        public BaseLayer(string layerName)
        {
            LAYER_NAME = layerName;
        }

        #endregion

        #region Base methods and properties

        public static Color UCOLOR = Color.Green;
        public static Color VCOLOR = Color.Blue;

        public int LayerIndex
        {
            get
            {
                var layerIndex = LAYER_INDEX;
                if (layerIndex == -1)
                {
                    layerIndex = Layers.LayerMethods.EnsureIsCreated(Doc, LAYER_NAME);
                }
                return layerIndex;
            }
        }

        public void EnsureIsCreated()
        {
            var index = LayerIndex; // enought!
        }

        public void Delete()
        {
            Layers.LayerMethods.DeleteLayer(Doc, LAYER_NAME);
        }

        public int Clear()
        {
            if (Viewport.DEBUG)
            {
                log.temp("*****   Layer clear " + LAYER_NAME);
            }
            return Layers.LayerMethods.Clear(Doc, LAYER_NAME);
        }

        public void Zoom(int fitFactor = 2)
        {
            Layers.LayerMethods.Zoom(Doc, LAYER_NAME, fitFactor);
        }

        public void UnselectAll()
        {
            Layers.LayerMethods.UnselectAll(Doc, LAYER_NAME);
        }

        private Color DefColor(Color color = default(Color))
        {
            return (color == default(Color))
                ? Color.Aqua
                : color;
        }

        #endregion


        public Guid AddNormal(BrepFace face)
        {
            if (!IS_ENABLED) return Guid.Empty;

            if (face.OuterLoop == null) return Guid.Empty;

            Point3d midPoint3d;
            var normal = face._GetNormalAndMidPoint3d(out midPoint3d);
            var multiplier = face._GetTotalLengthOfEdges() / 6;

            normal.Unitize();
            normal *= multiplier;
            var name = face.OrientationIsReversed ? "Face normal" : "normal";

            if (face.OrientationIsReversed)
            {
                var srf = face._Srf();
                var u = srf.Domain(0).Mid;
                var v = srf.Domain(1).Mid;
                var srfNormal = srf.NormalAt(u, v);
                srfNormal.Unitize();
                srfNormal *= multiplier * 0.8;
                AddNormal(srf.PointAt(u, v), srfNormal, Color.BurlyWood, "Surface normal");
            }

            return AddNormal(midPoint3d, normal, Color.Brown, name);
        }

        public Guid AddNormal(Point3d normalStartPoint, Vector3d normal, Color color = default(Color), string name = "Normal")
        {
            if (!IS_ENABLED) return Guid.Empty;
            color = DefColor(color);
            normal.Unitize();

            //Utils.HighlightLayer.AddPoint(doc, midPoint3d);
            //Utils.HighlightLayer.AddPoint(doc, normal + midPoint3d, Color.Red);
            var startPoint = normalStartPoint;
            var endPoint = normalStartPoint + normal;
            Guid id = Doc.Objects.AddLine(startPoint, endPoint, new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                Name = name,
                ColorSource = ObjectColorSource.ColorFromObject,
                ObjectColor = color,
                //DisplayOrder = 100,
                Visible = true,
                ObjectDecoration = ObjectDecoration.EndArrowhead
            }, null, true);
            AddTextPoint(name, endPoint + normal * 0.5, color);
            return id;
        }

        public Guid AddArrow(Point3d startPoint, Point3d endPoint, string name, Color color = default(Color))
        {
            if (!IS_ENABLED) return Guid.Empty;
            color = DefColor(color);

            Guid id = Doc.Objects.AddLine(startPoint, endPoint, new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                Name = name,
                ColorSource = ObjectColorSource.ColorFromObject,
                ObjectColor = color,
                //DisplayOrder = 100,
                Visible = true,
                ObjectDecoration = ObjectDecoration.EndArrowhead
                
            }, null, true);
            return id;
        }

        public Guid AddLine(Point3d startPoint, Point3d endPoint, Color color = default(Color))
        {
            if (!IS_ENABLED) return Guid.Empty;
            color = DefColor(color);

            Guid id = Doc.Objects.AddLine(startPoint, endPoint, new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                ColorSource = ObjectColorSource.ColorFromObject,
                ObjectColor = color,
                //DisplayOrder = 100,
                Visible = true,
                ObjectDecoration = ObjectDecoration.None
            }, null, true);
            return id;
        }

        public void AddVector(Vector3d tangent, Point3d atPoint, string name = "", double len = 0.5, Color color = default(Color))
        {
            if (!IS_ENABLED) return;
            tangent.Unitize();
            var endPoint = atPoint + tangent * len;
            AddPoint(atPoint, color);
            AddArrow(atPoint, endPoint, name, color);
            if (!String.IsNullOrEmpty(name))
            {
                AddTextPoint(name, endPoint + tangent * 0.1, color);
            }
        }

        public void AddTangent(Curve crv, string name, Color color = default(Color))
        {
            if (!IS_ENABLED) return;

            var TSet = new[] { crv.Domain.T0, crv.Domain.T1 };
            var TMultiplier = new[] { 0.45, 0.55 };
            var TSetStr = new[] { "T0", "T1" };
            for (int i = 0; i < TSet.Length; i++)
            {
                var T = TSet[i];
                var TStr = TSetStr[i];
                var tangent = crv.TangentAt(T);
                var startPoint = crv.PointAt(T);
                // var TangentStr = name + ": " + String.Format("Tangent at {0}={1}", TStr, T._ToStringX(2));
                var TangentStr = name + ": " + String.Format("Tangent at {0}", TStr);
                var len = 0.5 + TMultiplier[i];
                AddVector(tangent, startPoint, TangentStr, len, color);
            }
        }

        public void AddTangentToFace(Curve crv, BrepFace face, string name, Color color = default(Color))
        {
            if (!IS_ENABLED) return;

            Point3d point;
            Vector3d tangent;
            if (crv._TangentToFace(face, out point, out tangent))
            {
                var normalStr = name + ": Tangent to face";
                AddVector(tangent, point, normalStr, 1, color);
            }
            else
            {
                var temp = 0;
            }
        }


        public void AddPoints(IEnumerable<Point3d> points, Color color = default(Color))
        {
            foreach (var p in points)
            {
                AddPoint(p, color);
            }
        }

        public void AddCurveControlPoints(Curve crv, Surface srf = null, Color color = default(Color))
        {
            if (!IS_ENABLED) return;
            if (crv == null) return;
            color = DefColor(color);

            var points = crv.ToNurbsCurve().Points.Select(o => o.Location).ToList();

            if (crv.Dimension == 2)
            {
                if (srf == null)
                {
                    log.wrong("BaseLayer.AddControlPoints() - srf must be provided for 2d crvs.");
                    return;
                }
                points = points.Select(o => srf.PointAt(o.X, o.Y)).ToList();
            }

            Layers.Debug.AddPoints(points, color);
        }

        public Guid AddPoint(Point3d point, Color color = default(Color))
        {
            if (!IS_ENABLED) return Guid.Empty;

            color = DefColor(color);
            Guid id = Doc.Objects.AddPoint(point, new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                //Mode = ObjectMode.Locked,
                ColorSource = ObjectColorSource.ColorFromObject,
                ObjectColor = color,
                //DisplayOrder = 100,
                Visible = true
                //ObjectDecoration = ObjectDecoration.BothArrowhead,                    
            }, null, true);
            //var o = doc.Objects.Find(id);
            //if (o != null)
            //{
            //    o.Attributes.ColorSource = ObjectColorSource.ColorFromLayer;
            //}
            return id;
        }

        public Guid AddTextPoint(string text, Point3d point, Color color = default(Color))
        {
            if (!IS_ENABLED) return Guid.Empty;
            color = DefColor(color);
            var textDot = new TextDot(text, point)
            {
                //FontHeight = 10
            };

            Guid id = Doc.Objects.AddTextDot(textDot, new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                Name = text,
                //Mode = ObjectMode.Locked,
                ColorSource = ObjectColorSource.ColorFromObject,
                ObjectColor = color,
                Visible = true,
            }, null, true);
            return id;
        }


        public void AddCurve(Curve curve, string name, Color color = default(Color))
        {
            AddCurve(curve, color);
            var norm = new CurveNormalized(curve);
            AddTextPoint(name, norm.PointAtMid, color);
        }

        public Guid AddCurve(Curve curve, Color color = default(Color)
            , ObjectDecoration decoration = ObjectDecoration.None
            , int linetypeIndex = -1       // look mehod 'Utils.LineTypes.GetLyneTypeIndex(SFLineType.Dash);' for more information
            , int displayOrder = 1        // lines should be always visible when selecting surface and all lines in loops are shown
            )
        {
            if (!IS_ENABLED) return Guid.Empty;
            if (curve == null)
            {
                log.wrong("Attempt to add null curve to layer '{0}'", LAYER_NAME);
                return Guid.Empty;
            }

            color = DefColor(color);
            var newCurve = curve.DuplicateCurve();
            Guid id = Doc.Objects.AddCurve(newCurve, new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                //Mode = ObjectMode.Locked,  - this option will force color to be Gray
                ObjectColor = color,
                ColorSource = ObjectColorSource.ColorFromObject,
                DisplayOrder = displayOrder,
                //ObjectDecoration = ObjectDecoration.BothArrowhead,   
                ObjectDecoration = decoration,
                LinetypeIndex = linetypeIndex,  //var lineTypeIndex = Utils.LineTypes.GetLyneTypeIndex(SFLineType.Dash);
                LinetypeSource = ObjectLinetypeSource.LinetypeFromObject,
            }, null, true);
            //var o = Doc.Objects.Find(id);
            //if (o != null)
            //{
            //    o.Attributes.ColorSource = ObjectColorSource.ColorFromLayer;
            //}
            return id;
        }


        public Guid AddSurface(Surface srf, Color color = default(Color))
        {
            if (!IS_ENABLED) return Guid.Empty;

            //color = DefColor(color);
            if (color == default(Color)) color = Color.Coral;
            var newSrf = (Surface)srf.Duplicate();
            var attr = new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                //Mode = ObjectMode.Locked, - this option will force color to be Gray
                ObjectColor = color,
                ColorSource = ObjectColorSource.ColorFromObject
            };

            // Ovveride display mode to color surface http://developer.rhino3d.com/samples/rhinocommon/objectdisplaymode/
            DisplayModesManager.ObjectAttributes_SetDisplayModeOverride(attr, DisplayModeType.TopologyColoredSurfaces);


            //var newBrep = newSrf.ToBrep();
            //Guid id = Doc.Objects.AddBrep(newBrep, attr, null, true);

            Guid id = Doc.Objects.AddSurface(newSrf, attr, null, true);

            CreateMesh(id);

            return id;
        }

        public Guid AddFaceName(string name, BrepFace face, Color color = default(Color))
        {
            if (!IS_ENABLED) return Guid.Empty;

            if (color == default(Color))
            {
                color = Color.White;
            }

            color = DefColor(color);
            var textDot = new TextDot(name, face._GetCentroid())
            {
                FontHeight = 10,
                FontFace = "Tahoma",
            };

            Guid id = Doc.Objects.AddTextDot(textDot, new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                Name = name,
                //Mode = ObjectMode.Locked,
                ColorSource = ObjectColorSource.ColorFromObject,
                ObjectColor = color,
                Visible = true,
            }, null, true);

            return id;
        }

        public Guid AddFace(BrepFace face, Color color = default(Color)) // , int displayOrder = 0
        {
            if (!IS_ENABLED) return Guid.Empty;

            //color = DefColor(color);
            if (color == default(Color)) color = Color.Coral;
            var newBrep = face.DuplicateFace(true);

            var attr = new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                //Mode = ObjectMode.Locked, - this option will force color to be Gray
                //DisplayOrder = displayOrder,  doesnt work - why? no clue!
                ObjectColor = color,
                ColorSource = ObjectColorSource.ColorFromObject
            };

            // Ovveride display mode to color surface http://developer.rhino3d.com/samples/rhinocommon/objectdisplaymode/
            DisplayModesManager.ObjectAttributes_SetDisplayModeOverride(attr, DisplayModeType.TopologyColoredSurfaces);

            Guid id = Doc.Objects.AddBrep(newBrep, attr, null, true);

            // create mesh if it not created yet
            var facemesh = face.GetMesh(MeshType.Any);
            if (facemesh == null || facemesh.Faces.Count == 0)
            {
                CreateMesh(id);
            }

            return id;
        }

        public Guid AddMesh(Mesh mesh, Color color = default(Color))
        {
            if (!IS_ENABLED) return Guid.Empty;
            if (mesh == null) return Guid.Empty;
            color = DefColor(color);

            var attr = new ObjectAttributes
            {
                LayerIndex = LayerIndex,
                ObjectColor = color,
                ColorSource = ObjectColorSource.ColorFromObject
            };

            Guid id = Doc.Objects.AddMesh(mesh, attr, null, true);
            return id;
        }

        public void AddUVNet(Surface srf)
        {
            if (!IS_ENABLED) return;

            var domainU = srf.Domain(0);
            var domainV = srf.Domain(1);
            var LINES_COUNT = 10;

            string failReason;
            double deviation;

            for (int x = 0; x <= LINES_COUNT; x++)
            {
                var u = domainU.Min + domainU.Length * (x / (double)LINES_COUNT);
                var vCurve = srf._TryGetIsoCurve(1, u, false, out failReason, out deviation);
                if (vCurve != null)
                {
                    AddCurve(vCurve, VCOLOR);
                }
            }
            for (int y = 0; y <= LINES_COUNT; y++)
            {
                var v = domainV.Min + domainV.Length * (y / (double)LINES_COUNT);
                var uCurve = srf._TryGetIsoCurve(0, v, false, out failReason, out deviation);
                if (uCurve != null)
                {
                    AddCurve(uCurve, UCOLOR);
                }
            }
        }

        public void AddUVSideNames(Surface srf, IsoStatus activeSide = IsoStatus.None)
        {
            if (!IS_ENABLED) return;

            var domainU = srf.Domain(0);
            var domainV = srf.Domain(1);
            var uColor = UCOLOR;
            var vColor = VCOLOR;

            var uCurveMid = srf._GetUCurve(domainV.Mid);
            var vCurveMid = srf._GetVCurve(domainU.Mid);
            var shift = Math.Min(uCurveMid._GetLength_ThreadSafe(), vCurveMid._GetLength_ThreadSafe()) / 10;

            // add: West, East, North, South
            var pWest = srf._GetUCurve(domainV.Mid)._GetCurveExtEndPoint(CurveEnd.Start, shift * 3);
            var pEast = srf._GetUCurve(domainV.Mid)._GetCurveExtEndPoint(CurveEnd.End, shift * 3);
            var pSouth = srf._GetVCurve(domainU.Mid)._GetCurveExtEndPoint(CurveEnd.Start, shift * 3);
            var pNorth = srf._GetVCurve(domainU.Mid)._GetCurveExtEndPoint(CurveEnd.End, shift * 3);
            AddTextPoint("West", pWest, activeSide == IsoStatus.West ? Color.Red : Color.Silver);
            AddTextPoint("East", pEast, activeSide == IsoStatus.East ? Color.Red : Color.Silver);
            AddTextPoint("South", pSouth, activeSide == IsoStatus.South ? Color.Red : Color.Silver);
            AddTextPoint("North", pNorth, activeSide == IsoStatus.North ? Color.Red : Color.Silver);
        }

        public void AddUVArrows(Surface srf)
        {
            if (!IS_ENABLED) return;

            //Doc = Doc;
            var domainU = srf.Domain(0);
            var domainV = srf.Domain(1);
            var uColor = UCOLOR;
            var vColor = VCOLOR;

            var uCurveMid = srf._GetUCurve(domainV.Mid);
            var vCurveMid = srf._GetVCurve(domainU.Mid);
            var shift = Math.Min(uCurveMid._GetLength_ThreadSafe(), vCurveMid._GetLength_ThreadSafe()) / 10;


            //
            // create U
            //
            var uCurve = srf._GetUCurve(domainV.Min);
            var uCurveE = uCurve.Extend(CurveEnd.End, shift * 3, CurveExtensionStyle.Smooth) ?? uCurve;

            //
            // create V
            //
            var vCurve = srf._GetVCurve(domainU.Min);
            var vCurveE = vCurve.Extend(CurveEnd.End, shift * 3, CurveExtensionStyle.Smooth) ?? vCurve;

            //
            // add U arrow and text
            //
            AddCurve(uCurveE, uColor, ObjectDecoration.EndArrowhead);
            var puStart = srf._GetVCurve(domainU.Min)._GetCurveExtEndPoint(CurveEnd.Start, shift, vCurveMid);
            var puMiddle = vCurveMid._GetCurveExtEndPoint(CurveEnd.Start, shift);
            var puEnd = srf._GetVCurve(domainU.Max)._GetCurveExtEndPoint(CurveEnd.Start, shift, vCurveMid);
            AddTextPoint(UVtoText(domainU.Min), puStart, uColor);
            AddTextPoint("U", puMiddle, uColor);
            AddTextPoint(UVtoText(domainU.Max), puEnd, uColor);

            //
            // add V arrow and text
            //
            AddCurve(vCurveE, vColor, ObjectDecoration.EndArrowhead);
            var pvStart = srf._GetUCurve(domainV.Min)._GetCurveExtEndPoint(CurveEnd.Start, shift, uCurveMid);
            var pvMiddle = uCurveMid._GetCurveExtEndPoint(CurveEnd.Start, shift);
            var pvEnd = srf._GetUCurve(domainV.Max)._GetCurveExtEndPoint(CurveEnd.Start, shift, uCurveMid);
            AddTextPoint(UVtoText(domainV.Min), pvStart, vColor);
            AddTextPoint("V", pvMiddle, vColor);
            AddTextPoint(UVtoText(domainV.Max), pvEnd, vColor);

            // add center
            //var cDirection = vCurveE.PointAtStart - vCurve.PointAtStart;
            //var pCenter = vCurveE.PointAtStart + cDirection / 3;
            //var text = ".";
            //Utils.HighlightLayer.AddTextPoint(text, pCenter, Color.Black);
        }

        public void AddSeam(Surface srf, Color color = default(Color))
        {
            if (!IS_ENABLED) return;

            if (color == default(Color))
            {
                color = Color.Crimson;
            }

            var ss = new SurfaceSeams(srf);
            if (ss.HasSeams)
            {
                var uT0 = ss.U.Domain.T0;
                var uT1 = ss.U.Domain.T1;
                var vT0 = ss.V.Domain.T0;
                var vT1 = ss.V.Domain.T1;

                if (ss.U.IsAtT0 && ss.U.IsAtT1)
                {
                    AddCurve(srf._GetVCurve(ss.U.T0), "Seam on U at {0:0.00} & {1:0.00}"._Format(uT0, uT1), color);
                }
                if (ss.V.IsAtT0 && ss.V.IsAtT1)
                {
                    AddCurve(srf._GetUCurve(ss.V.T0), "Seam on V at {0:0.00} & {1:0.00}"._Format(vT0, vT1), color);
                }
            }
        }

        public void AddSurfaceUVContorlPoints(Surface srf, bool drawAsTextDot_U, bool drawAsTextDot_V, bool drawAsDot, bool drawAsWeights)
        {
            if (!IS_ENABLED) return;

            if (drawAsTextDot_U || drawAsTextDot_V || drawAsDot || drawAsWeights)
            {
                var srfNurb = srf.ToNurbsSurface();
                if (srfNurb != null)
                {
                    for (int iu = 0; iu < srfNurb.Points.CountU; iu++)
                    {
                        for (int iv = 0; iv < srfNurb.Points.CountV; iv++)
                        {
                            var cp = srfNurb.Points.GetControlPoint(iu, iv);
                            if (drawAsDot)
                            {
                                AddPoint(cp.Location, Color.Black);
                            }
                            if (drawAsTextDot_U || drawAsTextDot_V)
                            {
                                var text = drawAsTextDot_U ? iu.ToString() : iv.ToString();
                                var color = drawAsTextDot_U ? UCOLOR : VCOLOR;
                                AddTextPoint(text, cp.Location, color);
                            }
                            if (drawAsWeights)
                            {
                                //AddTextPoint(cp.Weight._ToStringX(3), cp.Location, Color.Black);
                                var text = "{0:0.000}"._Format(cp.Weight).Replace("1.000", "1");
                                AddTextPoint(text, cp.Location, Color.Black);
                            }
                        }
                    }
                }
            }
        }

        #region Private Methods

        private static string UVtoText(double value)
        {
            return String.Format("{0:0.00}", value).Replace(",00", "").Replace(".00", "");
        }

        private void CreateMesh(Guid id)
        {
            // create mesh for new object to avoid message in console 'Create meshes..."
            var o = Doc.Objects.Find(id);
            if (o != null)
            {
                o._CreateMesh();
            }
        }

        #endregion

    }
}
