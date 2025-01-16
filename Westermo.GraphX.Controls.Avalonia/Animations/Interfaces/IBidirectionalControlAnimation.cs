﻿namespace Westermo.GraphX.Controls.Avalonia.Animations
{
    public interface IBidirectionalControlAnimation
    {
        double Duration { get; set; }
        void AnimateVertexForward(VertexControl target);
        void AnimateVertexBackward(VertexControl target);
        void AnimateEdgeForward(EdgeControl target);
        void AnimateEdgeBackward(EdgeControl target);
    }
}