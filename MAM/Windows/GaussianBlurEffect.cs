using Windows.Graphics.Effects;

namespace MAM.Windows
{
    internal class GaussianBlurEffect : IGraphicsEffect
    {
        public string Name { get; set; }
        public object BorderMode { get; set; }
        public object Source { get; set; }
        public float BlurAmount { get; set; }
    }
}