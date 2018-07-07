using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConsoleGames
{
    public class TextEffectOptions
    {
        public string Text { get; set; }
        public float Top { get; set; }
        public float Left { get; set; }
        public int DurationMilliseconds { get; set; } 
    }

    public abstract class TextEffect : Lifetime
    {
        protected static readonly Random Random = new Random();

        public TextEffectOptions Options { get; set; }

        protected List<SpacialElement> matter = new List<SpacialElement>();
        public abstract void Start();
         

        public T Any<T>(params T[] choices)
        {
            return choices[Random.Next(0, choices.Length)];
        }

        public T Usually<T>(T rule, T exception, float usualPercentage = .9f)
        {
            if(Random.NextDouble() <= usualPercentage)
            {
                return rule;
            }
            else
            {
                return exception;
            }
        }
    }

    public class TextEffectReviver : ItemReviver
    {
        public string EffectName { get; private set; }
        public TextEffectReviver(string effectName)
        {
            this.EffectName = effectName;
        }

        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out SpacialElement hydratedElement)
        {
            var text = string.Empty;
            if(item.HasSimpleTag(EffectName) || item.HasValueTag(EffectName))
            {
                for(var x = 0; x < 500; x++)
                {
                    var next = allItems.Where(i => i.X == item.X+x && i.Y == item.Y).FirstOrDefault();
                    if(next != null)
                    {
                        next.Ignore = true;
                        text+=  next.Symbol;
                    }
                    else
                    {
                        break;
                    }
                }


                var delay = item.HasValueTag(EffectName) && int.TryParse(item.GetTagValue(EffectName), out int result) ? result : 0;
                var stay = item.HasValueTag("stay") && int.TryParse(item.GetTagValue("stay"), out int stayResult) ? stayResult : 10000;


                var effect = CreateInstance(EffectName);
                effect.Options = new TextEffectOptions()
                {
                    Left = item.X,
                    Top = item.Y,
                    Text = text,
                    DurationMilliseconds = stay,
                };
                hydratedElement = new InvisibleSpacialElement(delay,effect.Start);

                return true;
            }

            hydratedElement = null;
            return false;
        }

        private TextEffect CreateInstance(string name)
        {
            var candidate = Assembly.GetExecutingAssembly().ExportedTypes.Where(t => t.Name.ToLower() == name.ToLower()).FirstOrDefault();

            if(candidate == null)
            {
                Assembly.GetEntryAssembly().ExportedTypes.Where(t => t.Name.ToLower() == name.ToLower()).FirstOrDefault();
            }

            if(candidate == null)
            {
                throw new ArgumentException("Could not resolve text effect: "+name);
            }

            return (TextEffect)Activator.CreateInstance(candidate);
        }
    }

    public class InvisibleSpacialElement : SpacialElement
    {
        private int delayInMilliseconds;
        private Action init;
        public InvisibleSpacialElement(int delayInMilliseconds, Action init)
        {
            this.init = init;
            this.delayInMilliseconds = delayInMilliseconds;
        }

        public override void Initialize()
        {
            SpaceTime.CurrentSpaceTime.Add(TimeFunction.CreateDelayed(delayInMilliseconds, null, init: init));
            this.Lifetime.Dispose();
        }

        public override void Evaluate() { }
    }

    [SpacialElementBinding(typeof(InvisibleSpacialElement))]
    public class InvisibleRenderer : SpacialElementRenderer
    {
        public InvisibleRenderer()
        {
            IsVisible = false;
        }
    }
}
