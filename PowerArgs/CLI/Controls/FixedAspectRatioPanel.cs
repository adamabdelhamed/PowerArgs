using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A panel that centers it's child content and keeps that content at a fixed aspect ratio as this panel is resized.
    /// </summary>
    public class FixedAspectRatioPanel : ProtectedConsolePanel
    {
        private float widthOverHeight;
        private ConsoleControl content;

        /// <summary>
        /// Creates a fixed aspect ratio panel
        /// </summary>
        /// <param name="widthOverHeight">the aspect ratio defined as the width divided by the height</param>
        /// <param name="content">the content to center on this panel</param>
        public FixedAspectRatioPanel(float widthOverHeight, ConsoleControl content)
        {
            this.content = content;
            this.widthOverHeight = widthOverHeight;
            ProtectedPanel.Add(content).CenterBoth();
            this.SynchronizeForLifetime(nameof(Bounds), UpdateContentSize, this);
        }

        private void UpdateContentSize()
        {
            var w = (float)Width;
            var h = w / widthOverHeight;

            if(h > Height)
            {
                h = Height;
                w = h * widthOverHeight;
            }

            content.Width = Math.Min(Width, Geo.Round(w));
            content.Height = Math.Min(Height, Geo.Round(h));
        }
    }
}
