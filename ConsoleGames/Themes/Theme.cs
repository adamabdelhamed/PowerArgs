using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
namespace ConsoleGames
{
    /// <summary>
    /// Defines a set of theming rules that can target specific renderer types
    /// </summary>
    public class Theme : Lifetime
    {
        private Dictionary<Type, List<Action<ThemeAwareSpacialElementRenderer>>> themeProcessors = new Dictionary<Type, List<Action<ThemeAwareSpacialElementRenderer>>>();

        /// <summary>
        /// Binds this theme to the given app. It will process every renderer currently in the app. As long as this theme is alive (not disposed) it will also listen for new
        /// controls added to the app and process new renderers too.
        /// </summary>
        /// <param name="app">The app to bind to</param>
        public void Bind(ConsoleApp app)
        {
            app.LayoutRoot.Descendents.ForEach(c => ThemeThisControl(c));
            app.ControlAdded.SubscribeForLifetime(ThemeThisControl, this);
        }

        private void ThemeThisControl(ConsoleControl obj)
        {
            if(obj is ThemeAwareSpacialElementRenderer == false)
            {
                return;
            }

            if(themeProcessors.TryGetValue(obj.GetType(), out List<Action<ThemeAwareSpacialElementRenderer>> processors))
            {
                foreach(var processor in processors)
                {
                    processor((ThemeAwareSpacialElementRenderer)obj);
                }
            }
        }

        /// <summary>
        /// Adds a new rule that will be applied to every instance of T in the visual tree
        /// </summary>
        /// <typeparam name="T">the type of renderer to target</typeparam>
        /// <param name="themeAction">the theming action to take on the rendrer</param>
        protected void Add<T>(Action<T> themeAction) where T : ThemeAwareSpacialElementRenderer
        {
            if(themeProcessors.TryGetValue(typeof(T), out List<Action<ThemeAwareSpacialElementRenderer>> processorsForT) == false)
            {
                processorsForT = new List<Action<ThemeAwareSpacialElementRenderer>>();
                themeProcessors.Add(typeof(T), processorsForT);
            }

            processorsForT.Add((t) => themeAction((T)t));
        }
    }

    /// <summary>
    /// A renderer that can be themed
    /// </summary>
    public class ThemeAwareSpacialElementRenderer : SpacialElementRenderer { }

    /// <summary>
    /// A themeable renderer that has a uniform style
    /// </summary>
    public abstract class SingleStyleRenderer : ThemeAwareSpacialElementRenderer
    {
        protected abstract ConsoleCharacter DefaultStyle { get; }

        public ConsoleCharacter? Style { get; set; }

        public ConsoleCharacter EffectiveStyle => Style.HasValue ? Style.Value : DefaultStyle;

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = EffectiveStyle;
            context.FillRect(0, 0, Width, Height);
        }
    }
}
