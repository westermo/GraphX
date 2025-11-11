namespace Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

public class EfficientSugiyamaLayoutParameters : LayoutParametersBase
{
    private LayoutDirection _direction = LayoutDirection.TopToBottom;
    private double _layerDistance = 15.0;
    private double _vertexDistance = 15.0;
    private int _positionMode = -1;
    private bool _optimizeWidth;
    private double _widthPerHeight = 1.0;
    private bool _minimizeEdgeLength = true;
    internal const int MAX_PERMUTATIONS = 50;
    private SugiyamaEdgeRoutings _edgeRouting = SugiyamaEdgeRoutings.Orthogonal;

    /// <summary>
    /// Layout direction
    /// </summary>
    public LayoutDirection Direction
    {
        get => _direction;
        set
        {
            if (value == _direction)
                return;

            _direction = value;
            NotifyPropertyChanged("Direction");
        }
    }

    public double LayerDistance
    {
        get => _layerDistance;
        set
        {
            if (value == _layerDistance)
                return;

            _layerDistance = value;
            NotifyPropertyChanged("LayerDistance");
        }
    }

    public double VertexDistance
    {
        get => _vertexDistance;
        set
        {
            if (value == _vertexDistance)
                return;

            _vertexDistance = value;
            NotifyPropertyChanged("VertexDistance");
        }
    }

    public int PositionMode
    {
        get => _positionMode;
        set
        {
            if (value == _positionMode)
                return;

            _positionMode = value;
            NotifyPropertyChanged("PositionMode");
        }
    }

    public double WidthPerHeight
    {
        get => _widthPerHeight;
        set
        {
            if (value == _widthPerHeight)
                return;

            _widthPerHeight = value;
            NotifyPropertyChanged("WidthPerHeight");
        }
    }

    public bool OptimizeWidth
    {
        get => _optimizeWidth;
        set
        {
            if (value == _optimizeWidth)
                return;

            _optimizeWidth = value;
            NotifyPropertyChanged("OptimizeWidth");
        }
    }

    public bool MinimizeEdgeLength
    {
        get => _minimizeEdgeLength;
        set
        {
            if (value == _minimizeEdgeLength)
                return;

            _minimizeEdgeLength = value;
            NotifyPropertyChanged("MinimizeEdgeLength");
        }
    }

    public SugiyamaEdgeRoutings EdgeRouting
    {
        get => _edgeRouting;
        set
        {
            if (value == _edgeRouting)
                return;

            _edgeRouting = value;
            NotifyPropertyChanged("EdgeRouting");
        }
    }
}