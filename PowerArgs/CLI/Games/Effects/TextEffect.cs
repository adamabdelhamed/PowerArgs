using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PowerArgs;
namespace PowerArgs.Games
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
        protected class CharacterVisit
        {
            public char Symbol { get; set; }
            public int CharIndex { get; set; }
            public int Left { get; set; }
            public int Top { get; set; }
        }

        protected static readonly Random Random = new Random();

        public TextEffectOptions Options { get; set; }

        protected List<SpacialElement> matter = new List<SpacialElement>();
        public abstract void Start();
         

        protected IEnumerable<CharacterVisit> Characters
        {
            get
            {
                for (var i = 0; i < Options.Text.Length; i++)
                {
                    yield return new CharacterVisit()
                    {
                        CharIndex = i,
                        Left = (int)(Options.Left+i),
                        Symbol = Options.Text[i],
                        Top = (int)Options.Top
                    };
                }
            }
        }

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

        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
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


                var effect = SceneFactory.CreateInstance<TextEffect>(EffectName);
                effect.Options = new TextEffectOptions()
                {
                    Left = item.X,
                    Top = item.Y,
                    Text = text,
                    DurationMilliseconds = stay,
                };

                if (item.HasValueTag("triggerId") == false)
                {
                    hydratedElement = TimeFunction.CreateDelayed(delay, null, init: effect.Start);
                    return true;
                }
                else
                {
                    hydratedElement = TimeFunction.Create(null, () =>
                    {
                         var id = item.GetTagValue("triggerId");
                         var trigger = SpaceTime.CurrentSpaceTime.Elements.WhereAs<Trigger>().Where(t => t.Id == id).SingleOrDefault();

                         if (trigger == null)
                         {
                             throw new ArgumentException("No trigger with id: " + id);
                         }

                         trigger.Fired.SubscribeOnce((notused) =>
                         {
                             effect.Start();
                         });
                     });

                    return true;
                }
            }

            hydratedElement = null;
            return false;
        }
    }
}
