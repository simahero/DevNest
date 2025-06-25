using Microsoft.UI.Xaml.Media.Animation;

namespace DevNest.UI.Services
{
    public static class NavigationTransitions
    {
        public static SuppressNavigationTransitionInfo Suppress => new();

        public static SlideNavigationTransitionInfo SlideFromRight => new() { Effect = SlideNavigationTransitionEffect.FromRight };

        public static SlideNavigationTransitionInfo SlideFromLeft => new() { Effect = SlideNavigationTransitionEffect.FromLeft };

        public static SlideNavigationTransitionInfo SlideFromBottom => new() { Effect = SlideNavigationTransitionEffect.FromBottom };

        public static DrillInNavigationTransitionInfo DrillIn => new();

        public static EntranceNavigationTransitionInfo Entrance => new();

        public static ContinuumNavigationTransitionInfo Continuum(Microsoft.UI.Xaml.UIElement continuityElement)
        {
            return new ContinuumNavigationTransitionInfo { ExitElement = continuityElement };
        }
    }
}
