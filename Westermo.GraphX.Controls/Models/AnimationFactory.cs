using System;
using Westermo.GraphX.Controls.Animations;

namespace Westermo.GraphX.Controls.Models
{
    public static class AnimationFactory
    {
        /// <summary>
        /// Create move animation by supplied type
        /// </summary>
        /// <param name="type">Animation type</param>
        /// <param name="duration">Animation duration</param>
        public static MoveAnimationBase? CreateMoveAnimation(MoveAnimation type, TimeSpan duration)
        {
            return type switch
            {
                MoveAnimation.None => null,
                MoveAnimation.Move => new MoveSimpleAnimation(duration),
                MoveAnimation.Fade => new MoveFadeAnimation(duration),
                _ => null,
            };
        }

        public static IOneWayControlAnimation? CreateDeleteAnimation(DeleteAnimation type, double duration = .3)
        {
            return type switch
            {
                DeleteAnimation.None => null,
                DeleteAnimation.Shrink => new DeleteShrinkAnimation(duration),
                DeleteAnimation.Fade => new DeleteFadeAnimation(duration),
                _ => null,
            };
        }

        public static IBidirectionalControlAnimation? CreateMouseOverAnimation(MouseOverAnimation type, double duration = .3)
        {
            return type switch
            {
                MouseOverAnimation.None => null,
                MouseOverAnimation.Scale => new MouseOverScaleAnimation(duration),
                _ => null,
            };
        }
    }
}
