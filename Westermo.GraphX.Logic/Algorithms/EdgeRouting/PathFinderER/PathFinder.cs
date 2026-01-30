//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
//
//  Email:  gustavo_franco@hotmail.com
//
//  Copyright (C) 2006 Franco, Gustavo 
//
#define DEBUGON

using System;
using System.Collections.Generic;
using Westermo.GraphX.Measure;

namespace Westermo.GraphX.Logic.Algorithms.EdgeRouting;

#region Structs

public struct PathFinderNode
{
    #region Variables Declaration
    public int     F;
    public int     G;
    public int     H;  // f = gone + heuristic
    public int     X;
    public int     Y;
    public int     PX; // Parent
    public int     PY;
    #endregion
}
#endregion

#region Enum

public enum PathFinderNodeType
{
    Start   = 1,
    End     = 2,
    Open    = 4,
    Close   = 8,
    Current = 16,
    Path    = 32
}

public enum HeuristicFormula
{
    Manhattan           = 1,
    MaxDXDY             = 2,
    DiagonalShortCut    = 3,
    Euclidean           = 4,
    EuclideanNoSQR      = 5,
    Custom1             = 6
}
#endregion

#region Delegates
public delegate void PathFinderDebugHandler(int fromX, int fromY, int x, int y, PathFinderNodeType type, int totalCost, int cost);
#endregion

public class PathFinder : IPathFinder
{
    // [System.Runtime.InteropServices.DllImport("KERNEL32.DLL", EntryPoint="RtlZeroMemory")]
    // public unsafe static extern bool ZeroMemory(byte* destination, int length);

    #region Static Direction Arrays
    // Pre-allocated direction arrays to avoid per-call allocation.
    // WARNING: These static readonly arrays are shared across all PathFinder instances and must never be modified.
    private static readonly sbyte[,] DiagonalDirections = { {0,-1}, {1,0}, {0,1}, {-1,0}, {1,-1}, {1,1}, {-1,1}, {-1,-1} };
    private static readonly sbyte[,] CardinalDirections = { {0,-1}, {1,0}, {0,1}, {-1,0} };
    #endregion

    #region Events
    public event PathFinderDebugHandler PathFinderDebug;
    #endregion

    #region Variables Declaration
    private readonly MatrixItem[,] mGrid;
    private readonly PriorityQueueB<PathFinderNode>  mOpen                   = new(new ComparePFNode());
    private readonly List<PathFinderNode>            mClose                  = [];
    // 2D arrays for O(1) grid-indexed lookup (much faster than HashSet tuple hashing)
    private byte[,] mNodeStatus;  // 0 = unvisited, 1 = open, 2 = closed
    private int[,] mGScores;      // Best G score for each grid cell
    private const byte STATUS_UNVISITED = 0;
    private const byte STATUS_OPEN = 1;
    private const byte STATUS_CLOSED = 2;
    private bool                            mStop;
    private bool                            mStopped                = true;
    private int                             mHoriz;
    private HeuristicFormula                mFormula                = HeuristicFormula.Manhattan;
    private bool                            mDiagonals              = true;
    private int                             mHEstimate              = 2;
    private bool                            mPunishChangeDirection;
    private bool                            mTieBreaker;
    private bool                            mHeavyDiagonals;
    private int                             mSearchLimit            = 2000;
    //private double                          mCompletedTime          = 0; //not used
    private bool                            mDebugProgress;
    private bool                            mDebugFoundPath;
    #endregion

    #region Constructors
    public PathFinder(MatrixItem[,] grid)
    {
        if (grid == null)
            throw new Exception("Grid cannot be null");

        mGrid = grid;
    }
    #endregion

    #region Properties
    public bool Stopped => mStopped;

    public HeuristicFormula Formula
    {
        get => mFormula;
        set => mFormula = value;
    }

    public bool Diagonals
    {
        get => mDiagonals;
        set => mDiagonals = value;
    }

    public bool HeavyDiagonals
    {
        get => mHeavyDiagonals;
        set => mHeavyDiagonals = value;
    }

    public int HeuristicEstimate
    {
        get => mHEstimate;
        set => mHEstimate = value;
    }

    public bool PunishChangeDirection
    {
        get => mPunishChangeDirection;
        set => mPunishChangeDirection = value;
    }

    public bool TieBreaker
    {
        get => mTieBreaker;
        set => mTieBreaker = value;
    }

    public int SearchLimit
    {
        get => mSearchLimit;
        set => mSearchLimit = value;
    }

    /*public double CompletedTime
    {
        get { return mCompletedTime; }
        set { mCompletedTime = value; }
    }*/

    public bool DebugProgress
    {
        get => mDebugProgress;
        set => mDebugProgress = value;
    }

    public bool DebugFoundPath
    {
        get => mDebugFoundPath;
        set => mDebugFoundPath = value;
    }
    #endregion

    #region Methods
    public void FindPathStop()
    {
        mStop = true;
    }

    public List<PathFinderNode> FindPath(Point start, Point end)
    {
        //!PCL-NON-COMPL! HighResolutionTime.Start();

        PathFinderNode parentNode;
        var found  = false;
        var  gridX  = mGrid.GetUpperBound(0);
        var  gridY  = mGrid.GetUpperBound(1);

        mStop       = false;
        mStopped    = false;
        mOpen.Clear();
        mClose.Clear();
        
        // Initialize or clear the lookup arrays
        if (mNodeStatus == null || mNodeStatus.GetLength(0) != gridX + 1 || mNodeStatus.GetLength(1) != gridY + 1)
        {
            mNodeStatus = new byte[gridX + 1, gridY + 1];
            mGScores = new int[gridX + 1, gridY + 1];
        }
        else
        {
            Array.Clear(mNodeStatus);
            // No need to clear mGScores - we only read it when mNodeStatus indicates open
        }

#if DEBUGON
        if (mDebugProgress && PathFinderDebug != null)
            PathFinderDebug(0, 0, (int)start.X, (int)start.Y, PathFinderNodeType.Start, -1, -1);
        if (mDebugProgress && PathFinderDebug != null)
            PathFinderDebug(0, 0, (int)end.X, (int)end.Y, PathFinderNodeType.End, -1, -1);
#endif

        // Use pre-allocated static direction arrays
        var direction = mDiagonals ? DiagonalDirections : CardinalDirections;

        parentNode.G         = 0;
        parentNode.H         = mHEstimate;
        parentNode.F         = parentNode.G + parentNode.H;
        parentNode.X         = (int)start.X;
        parentNode.Y         = (int)start.Y;
        parentNode.PX        = parentNode.X;
        parentNode.PY        = parentNode.Y;
        mOpen.Push(parentNode);
        mNodeStatus[parentNode.X, parentNode.Y] = STATUS_OPEN;
        mGScores[parentNode.X, parentNode.Y] = parentNode.G;
        while(mOpen.Count > 0 && !mStop)
        {
            parentNode = mOpen.Pop();

#if DEBUGON
            if (mDebugProgress && PathFinderDebug != null)
                PathFinderDebug(0, 0, parentNode.X, parentNode.Y, PathFinderNodeType.Current, -1, -1);
#endif

            if (parentNode.X == end.X && parentNode.Y == end.Y)
            {
                mClose.Add(parentNode);
                found = true;
                break;
            }

            if (mClose.Count > mSearchLimit)
            {
                mStopped = true;
                return null;
            }

            if (mPunishChangeDirection)
                mHoriz = parentNode.X - parentNode.PX; 

            //Lets calculate each successors
            for (var i=0; i<(mDiagonals ? 8 : 4); i++)
            {
                PathFinderNode newNode;
                newNode.X = parentNode.X + direction[i,0];
                newNode.Y = parentNode.Y + direction[i,1];

                if (newNode.X < 0 || newNode.Y < 0 || newNode.X >= gridX || newNode.Y >= gridY)
                    continue;

                int newG;
                if (mHeavyDiagonals && i>3)
                    newG = parentNode.G + (int) (mGrid[newNode.X, newNode.Y].Weight * 2.41);
                else
                    newG = parentNode.G + mGrid[newNode.X, newNode.Y].Weight;


                if (newG == parentNode.G)
                {
                    //Unbrekeable
                    continue;
                }

                if (mPunishChangeDirection)
                {
                    if (newNode.X - parentNode.X != 0)
                    {
                        if (mHoriz == 0)
                            newG += 20;
                    }
                    if (newNode.Y - parentNode.Y != 0)
                    {
                        if (mHoriz != 0)
                            newG += 20;

                    }
                }

                // O(1) lookup using 2D arrays - direct grid indexing (no hashing overhead)
                var status = mNodeStatus[newNode.X, newNode.Y];
                
                // Check if already closed
                if (status == STATUS_CLOSED)
                    continue;
                
                // Check if already in open set with better or equal G score
                if (status == STATUS_OPEN && mGScores[newNode.X, newNode.Y] <= newG)
                    continue;

                newNode.PX      = parentNode.X;
                newNode.PY      = parentNode.Y;
                newNode.G       = newG;

                switch(mFormula)
                {
                    default:
                    case HeuristicFormula.Manhattan:
                        newNode.H       = mHEstimate * (Math.Abs(newNode.X - (int)end.X) + Math.Abs(newNode.Y - (int)end.Y));
                        break;
                    case HeuristicFormula.MaxDXDY:
                        newNode.H = mHEstimate * Math.Max(Math.Abs(newNode.X - (int)end.X), Math.Abs(newNode.Y - (int)end.Y));
                        break;
                    case HeuristicFormula.DiagonalShortCut:
                        var h_diagonal = Math.Min(Math.Abs(newNode.X - (int)end.X), Math.Abs(newNode.Y - (int)end.Y));
                        var h_straight = Math.Abs(newNode.X - (int)end.X) + Math.Abs(newNode.Y - (int)end.Y);
                        newNode.H       = mHEstimate * 2 * h_diagonal + mHEstimate * (h_straight - 2 * h_diagonal);
                        break;
                    case HeuristicFormula.Euclidean:
                        newNode.H       = (int) (mHEstimate * Math.Sqrt(Math.Pow(newNode.X - end.X , 2) + Math.Pow(newNode.Y - end.Y, 2)));
                        break;
                    case HeuristicFormula.EuclideanNoSQR:
                        newNode.H       = (int) (mHEstimate * (Math.Pow(newNode.X - end.X , 2) + Math.Pow(newNode.Y - end.Y, 2)));
                        break;
                    case HeuristicFormula.Custom1:
                        var dxy       = new Point(Math.Abs(end.X - newNode.X), Math.Abs(end.Y - newNode.Y));
                        var Orthogonal  = (int)Math.Abs(dxy.X - dxy.Y);
                        var Diagonal    = (int)Math.Abs((dxy.X + dxy.Y - Orthogonal) / 2);
                        newNode.H       = mHEstimate * (int)(Diagonal + Orthogonal + dxy.X + dxy.Y);
                        break;
                }
                if (mTieBreaker)
                {
                    var dx1 = parentNode.X - end.X;
                    var dy1 = parentNode.Y - end.Y;
                    var dx2 = start.X - end.X;
                    var dy2 = start.Y - end.Y;
                    var cross = (int)Math.Abs(dx1 * dy2 - dx2 * dy1);
                    newNode.H = (int) (newNode.H + cross * 0.001);
                }
                newNode.F       = newNode.G + newNode.H;

#if DEBUGON
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(parentNode.X, parentNode.Y, newNode.X, newNode.Y, PathFinderNodeType.Open, newNode.F, newNode.G);
#endif
                    

                //It is faster if we leave the open node in the priority queue
                //When it is removed, all nodes around will be closed, it will be ignored automatically
                mOpen.Push(newNode);
                mNodeStatus[newNode.X, newNode.Y] = STATUS_OPEN;
                mGScores[newNode.X, newNode.Y] = newG;
            }

            mClose.Add(parentNode);
            mNodeStatus[parentNode.X, parentNode.Y] = STATUS_CLOSED;

#if DEBUGON
            if (mDebugProgress && PathFinderDebug != null)
                PathFinderDebug(0, 0, parentNode.X, parentNode.Y, PathFinderNodeType.Close, parentNode.F, parentNode.G);
#endif
        }

        //mCompletedTime = HighResolutionTime.GetTime();
        if (found)
        {
            var fNode = mClose[mClose.Count - 1];
            for(var i=mClose.Count - 1; i>=0; i--)
            {
                if (fNode.PX == mClose[i].X && fNode.PY == mClose[i].Y || i == mClose.Count - 1)
                {
#if DEBUGON
                    if (mDebugFoundPath && PathFinderDebug != null)
                        PathFinderDebug(fNode.X, fNode.Y, mClose[i].X, mClose[i].Y, PathFinderNodeType.Path, mClose[i].F, mClose[i].G);
#endif
                    fNode = mClose[i];
                }
                else
                    mClose.RemoveAt(i);
            }
            mStopped = true;
            return mClose;
        }
        mStopped = true;
        return null;
    }
    #endregion

    #region Inner Classes
    internal class ComparePFNode : IComparer<PathFinderNode>
    {
        #region IComparer Members
        public int Compare(PathFinderNode x, PathFinderNode y)
        {
            if (x.F > y.F)
                return 1;
            else if (x.F < y.F)
                return -1;
            return 0;
        }
        #endregion
    }
    #endregion


    public double CompletedTime { get; set; }
}