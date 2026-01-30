using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Westermo.GraphX.Measure;
using Westermo.GraphX.Common;
using Westermo.GraphX.Common.Exceptions;
using Westermo.GraphX.Common.Interfaces;
using QuikGraph;

namespace Westermo.GraphX.Logic.Algorithms.EdgeRouting;

public class SimpleEdgeRouting<TVertex, TEdge, TGraph> : EdgeRoutingAlgorithmBase<TVertex, TEdge, TGraph>
    where TGraph : class, IMutableBidirectionalGraph<TVertex, TEdge>
    where TEdge : class, IGraphXEdge<TVertex>
    where TVertex : class, IGraphXVertex
{
    public SimpleEdgeRouting(TGraph graph, IDictionary<TVertex, Point> vertexPositions,
        IDictionary<TVertex, Rect> vertexSizes, IEdgeRoutingParameters parameters = null)
        : base(graph, vertexPositions, vertexSizes, parameters)
    {
        if (parameters is not SimpleERParameters erParameters) return;
        drawback_distance = erParameters.BackStep;
        side_distance = erParameters.SideStep;
    }

    public override void UpdateVertexData(TVertex vertex, Point position, Rect size)
    {
        VertexPositions.AddOrUpdate(vertex, position);
        VertexSizes.AddOrUpdate(vertex, size);
    }

    public override Point[] ComputeSingle(TEdge edge)
    {
        EdgeRoutingTest(edge, CancellationToken.None);
        return EdgeRoutes.TryGetValue(edge, value: out var route) ? route : null;
    }

    public override void Compute(CancellationToken cancellationToken)
    {
        EdgeRoutes.Clear();
        // Pre-compute inflated vertex sizes once for all edges
        BuildInflatedSizesCache();
        foreach (var item in Graph.Edges)
            EdgeRoutingTest(item, cancellationToken);
        // Clear cache after computation to free memory
        _inflatedSizesCache = null;
    }

    private readonly double drawback_distance = 10;
    private readonly double side_distance = 5;
    private readonly double vertex_margin_distance = 35;

    // Cached inflated sizes to avoid repeated dictionary allocations per edge
    private Dictionary<TVertex, Rect> _inflatedSizesCache;

    /// <summary>
    /// Pre-builds the inflated sizes cache once per Compute() call.
    /// </summary>
    private void BuildInflatedSizesCache()
    {
        var inflateAmount = vertex_margin_distance * 2;
        _inflatedSizesCache = new Dictionary<TVertex, Rect>(VertexSizes.Count);
        foreach (var kvp in VertexSizes)
        {
            var inflated = kvp.Value;
            inflated.Inflate(inflateAmount, inflateAmount);
            _inflatedSizesCache[kvp.Key] = inflated;
        }
    }

    /// <summary>
    /// Gets vertices to check for intersection, excluding source and target.
    /// Uses cached inflated sizes instead of creating new dictionaries per edge.
    /// </summary>
    private IEnumerable<TVertex> GetVerticesToCheck(TEdge ctrl)
    {
        var sourceId = ctrl.Source.ID;
        var targetId = ctrl.Target.ID;
        foreach (var vertex in VertexSizes.Keys)
        {
            if (vertex.ID != sourceId && vertex.ID != targetId)
                yield return vertex;
        }
    }

    /// <summary>
    /// Gets the inflated rect for a vertex from cache, or computes it for single edge routing.
    /// </summary>
    private Rect GetInflatedRect(TVertex vertex)
    {
        if (_inflatedSizesCache != null && _inflatedSizesCache.TryGetValue(vertex, out var cached))
            return cached;
        
        // Fallback for ComputeSingle - compute on demand
        var rect = VertexSizes[vertex];
        rect.Inflate(vertex_margin_distance * 2, vertex_margin_distance * 2);
        return rect;
    }

    private void EdgeRoutingTest(TEdge ctrl, CancellationToken cancellationToken)
    {
        //bad edge data check
        if (ctrl.Source.ID == -1 || ctrl.Target.ID == -1)
            throw new GX_InvalidDataException(
                "SimpleEdgeRouting() -> You must assign unique ID for each vertex to use SimpleER algo!");
        if (ctrl.Source.ID == ctrl.Target.ID ||
            !VertexSizes.TryGetValue(ctrl.Source, out var ss) ||
            !VertexSizes.TryGetValue(ctrl.Target, out var es)) return;
        var startPoint = new Point(ss.X + ss.Width * 0.5, ss.Y + ss.Height * 0.5);
        var endPoint = new Point(es.X + es.Width * 0.5, es.Y + es.Height * 0.5);

        if (startPoint == endPoint) return;

        // Use HashSet to track checked/remaining vertices instead of dictionary copies
        var verticesToCheck = new HashSet<TVertex>(GetVerticesToCheck(ctrl));
        var remainingVertices = new HashSet<TVertex>(verticesToCheck);

        var tempList = new List<Point> { startPoint };

        var haveIntersections = true;

        //while we have some intersections - proceed
        while (haveIntersections)
        {
            var curDrawback = drawback_distance;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get first vertex from checklist using enumerator (avoids LINQ allocation)
                TVertex item = default;
                foreach (var v in verticesToCheck)
                {
                    item = v;
                    break;
                }
                
                //set last route point as current start point
                startPoint = tempList[tempList.Count - 1];
                if (item == null)
                {
                    //checked all vertices and no intersection was found - quit
                    haveIntersections = false;
                    break;
                }

                var r = GetInflatedRect(item);
                Point checkpoint;
                //check for intersection point. if none found - remove vertex from checklist
                if (GetIntersectionPoint(r, startPoint, endPoint, out checkpoint) == -1)
                {
                    verticesToCheck.Remove(item);
                    continue;
                }

                var mainVector = new Vector(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
                double X;
                //calculate drawback X coordinate
                if (Math.Abs(startPoint.X - checkpoint.X) < curDrawback)
                    X = startPoint.X;
                else if (startPoint.X < checkpoint.X) X = checkpoint.X - curDrawback;
                else X = checkpoint.X + curDrawback;
                //calculate drawback Y coordinate
                double Y;
                if (Math.Abs(startPoint.Y - checkpoint.Y) < curDrawback)
                    Y = startPoint.Y;
                else if (startPoint.Y < checkpoint.Y) Y = checkpoint.Y - curDrawback;
                else Y = checkpoint.Y + curDrawback;
                //set drawback checkpoint
                checkpoint = new Point(X, Y);
                var isStartPoint = checkpoint == startPoint;

                var routeFound = false;
                var viceVersa = false;
                var counter = 1;
                var joint = new Point();
                bool? blocked_direction = null;
                while (!routeFound)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    //choose opposite vector side each cycle
                    var signedDistance = viceVersa ? side_distance : -side_distance;
                    //get new point coordinate
                    joint = new Point(
                        checkpoint.X + signedDistance * counter * (mainVector.Y / mainVector.Length),
                        checkpoint.Y - signedDistance * counter * (mainVector.X / mainVector.Length));

                    //now check if new point is in some other vertex
                    var iresult = false;
                    var forcedBreak = false;
                    foreach (var vertex in remainingVertices)
                    {
                        if (GetInflatedRect(vertex).Contains(joint))
                        {
                            iresult = true;
                            //block this side direction
                            if (blocked_direction == null)
                                blocked_direction = viceVersa;
                            else
                            {
                                //both sides blocked - need to drawback
                                forcedBreak = true;
                            }
                            break;
                        }
                    }

                    if (forcedBreak) break;

                    //get vector intersection if its ok
                    if (!iresult) iresult = IsIntersected(r, joint, endPoint);

                    //if no vector intersection - we've found it!
                    if (!iresult)
                    {
                        routeFound = true;
                        blocked_direction = null;
                    }
                    else
                    {
                        //still have an intersection with current vertex
                        haveIntersections = true;
                        //skip point search if too many attempts was made (bad logic hack)
                        if (counter > 300) break;
                        counter++;
                        //switch vector search side
                        if (blocked_direction == null || blocked_direction == viceVersa)
                            viceVersa = !viceVersa;
                    }
                }

                //if blocked and this is not start point (nowhere to drawback) - then increase drawback distance
                if (blocked_direction != null && !isStartPoint)
                {
                    //search has been blocked - need to drawback
                    curDrawback += drawback_distance;
                }
                else
                {
                    //add new route point if we found it
                    // if(routeFound) 
                    tempList.Add(joint);
                    remainingVertices.Remove(item);
                }

                //remove currently evaded obstacle vertex from the checklist
                verticesToCheck.Remove(item);
            }

            //assign possible left vertices as a new checklist if any intersections was found
            if (haveIntersections)
            {
                verticesToCheck.Clear();
                foreach (var v in remainingVertices)
                    verticesToCheck.Add(v);
            }
        }
        //finally, add an end route point

        tempList.Add(endPoint);


        EdgeRoutes[ctrl] = tempList.Count > 2 ? [.. tempList] : null;
    }

    #region Math helper implementation

    public static Point GetCloserPoint(Point start, Point a, Point b)
    {
        var r1 = GetDistance(start, a);
        var r2 = GetDistance(start, b);
        return r1 < r2 ? a : b;
    }

    public static double GetDistance(Point a, Point b)
    {
        return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
    }

    public static Sides GetIntersectionData(Rect r, Point p)
    {
        return new Sides()
            { Left = p.X < r.Left, Right = p.X > r.Right, Bottom = p.Y > r.Bottom, Top = p.Y < r.Top };
    }

    public static bool IsIntersected(Rect r, Point a, Point b)
    {
        // var start = new Point(a.X, a.Y);
        /* код конечных точек отрезка */
        var codeA = GetIntersectionData(r, a);
        var codeB = GetIntersectionData(r, b);

        if (codeA.IsInside() && codeB.IsInside())
            return true;

        /* пока одна из точек отрезка вне прямоугольника */
        while (!codeA.IsInside() || !codeB.IsInside())
        {
            /* если обе точки с одной стороны прямоугольника, то отрезок не пересекает прямоугольник */
            if (codeA.SameSide(codeB))
                return false;

            /* выбираем точку c с ненулевым кодом */
            Sides code;
            Point c; /* одна из точек */
            if (!codeA.IsInside())
            {
                code = codeA;
                c = a;
            }
            else
            {
                code = codeB;
                c = b;
            }

            /* если c левее r, то передвигаем c на прямую x = r->x_min
               если c правее r, то передвигаем c на прямую x = r->x_max */
            if (code.Left)
            {
                c.Y += (a.Y - b.Y) * (r.Left - c.X) / (a.X - b.X);
                c.X = r.Left;
            }
            else if (code.Right)
            {
                c.Y += (a.Y - b.Y) * (r.Right - c.X) / (a.X - b.X);
                c.X = r.Right;
            } /* если c ниже r, то передвигаем c на прямую y = r->y_min
                если c выше r, то передвигаем c на прямую y = r->y_max */
            else if (code.Bottom)
            {
                c.X += (a.X - b.X) * (r.Bottom - c.Y) / (a.Y - b.Y);
                c.Y = r.Bottom;
            }
            else if (code.Top)
            {
                c.X += (a.X - b.X) * (r.Top - c.Y) / (a.Y - b.Y);
                c.Y = r.Top;
            }

            /* обновляем код */
            if (code == codeA)
            {
                a = c;
                codeA = GetIntersectionData(r, a);
            }
            else
            {
                b = c;
                codeB = GetIntersectionData(r, b);
            }
        }

        return true;
    }

    public static int GetIntersectionPoint(Rect r, Point a, Point b, out Point pt)
    {
        Sides code;
        Point c; /* одна из точек */
        var start = new Point(a.X, a.Y);
        /* код конечных точек отрезка */
        var code_a = GetIntersectionData(r, a);
        var code_b = GetIntersectionData(r, b);

        /* пока одна из точек отрезка вне прямоугольника */
        while (!code_a.IsInside() || !code_b.IsInside())
        {
            /* если обе точки с одной стороны прямоугольника, то отрезок не пересекает прямоугольник */
            if (code_a.SameSide(code_b))
            {
                pt = new Point();
                return -1;
            }

            /* выбираем точку c с ненулевым кодом */
            if (!code_a.IsInside())
            {
                code = code_a;
                c = a;
            }
            else
            {
                code = code_b;
                c = b;
            }

            /* если c левее r, то передвигаем c на прямую x = r->x_min
               если c правее r, то передвигаем c на прямую x = r->x_max */
            if (code.Left)
            {
                c.Y += (a.Y - b.Y) * (r.Left - c.X) / (a.X - b.X);
                c.X = r.Left;
            }
            else if (code.Right)
            {
                c.Y += (a.Y - b.Y) * (r.Right - c.X) / (a.X - b.X);
                c.X = r.Right;
            } /* если c ниже r, то передвигаем c на прямую y = r->y_min
                если c выше r, то передвигаем c на прямую y = r->y_max */
            else if (code.Bottom)
            {
                c.X += (a.X - b.X) * (r.Bottom - c.Y) / (a.Y - b.Y);
                c.Y = r.Bottom;
            }
            else if (code.Top)
            {
                c.X += (a.X - b.X) * (r.Top - c.Y) / (a.Y - b.Y);
                c.Y = r.Top;
            }

            /* обновляем код */
            if (code == code_a)
            {
                a = c;
                code_a = GetIntersectionData(r, a);
            }
            else
            {
                b = c;
                code_b = GetIntersectionData(r, b);
            }
        }

        pt = GetCloserPoint(start, a, b);
        return 0;
    }

    public sealed class Sides
    {
        public bool Left;
        public bool Right;
        public bool Top;
        public bool Bottom;

        public bool IsInside()
        {
            return Left == false && Right == false && Top == false && Bottom == false;
        }

        public bool SameSide(Sides o)
        {
            return (Left && o.Left) || (Right && o.Right) || (Top && o.Top)
                   || (Bottom && o.Bottom);
        }
    }

    #endregion
}