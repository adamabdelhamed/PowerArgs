using PowerArgs.Games;
using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DemoGame
{
    public class GameIntro : ICutScene
    {
        public static readonly Level Level = new Level() { Width = 100, Height = 30, Items = new List<LevelItem>()
        {
            new LevelItem() { Symbol = 'M', X = 50, Y = 15, Width = 0, Height = 0, Tags = new List<string>() { "main-character" } },
            new LevelItem() { Tags = new List<string>() { "cutscene:GameIntro" } }
        },
        };
        
        public void Start()
        {
            var spaceTime = SpaceTime.CurrentSpaceTime;
            var app = spaceTime.Application as DemoGameApp;
            app.MainCharacter.CurrentSpeedPercentage = .25f;
            app.MainCharacter.Inventory.Items.Clear();
            app.MainCharacter.Inventory.Items.Add(new RPGLauncher() { AmmoAmount = 100 });
            spaceTime.Delay(RenderFriendliesAndZombie, 500);

            spaceTime.Delay(()=>
            {
                WhenNoMoreFriendlies().Then(EnterHero);
            }, 2500);
        }

        private static Random rand = new Random();

        private static char RandomLetter()
        {
            var letters = new char[] { 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L' };
            var i = rand.Next(0, letters.Length);
            return letters[i];
        }

        private Promise WhenNoMoreFriendlies()
        {
            var d = Deferred.Create();

            ITimeFunction t = null; ;
            t = TimeFunction.Create(() =>
            {
                if(SpaceTime.CurrentSpaceTime.Elements.WhereAs<Friendly>().Count() == 0)
                {
                    t.Lifetime.Dispose();
                    d.Resolve();
                }
            }, rate: TimeSpan.FromSeconds(.1));
            SpaceTime.CurrentSpaceTime.Add(t);
            return d.Promise;
        }

        private void EnterHero()
        {
            MainCharacter.Current.ResizeTo(1, 1);
            MainCharacter.Current.Tags.Add("indestructible");
            for (var i = 0; i < 100; i++)
            {
                SpaceTime.CurrentSpaceTime.Delay(()=>
                {
                    if (MainCharacter.Current.Target != null)
                    {
                        MainCharacter.Current.Inventory.ExplosiveWeapon.TryFire();
                    }
                }, 250 * i);
            }
        }

        private void RenderFriendliesAndZombie()
        {
            var zombie = SpaceTime.CurrentSpaceTime.Add(new Enemy() { Symbol = 'Z' });
            var zBot = new Bot(zombie);
            zBot.Strategy = new MoveTowardsEnemy();
            zombie.MoveTo(SpaceTime.CurrentSpaceTime.Width * .5f, SpaceTime.CurrentSpaceTime.Height * .5f);

            for (var x = SpaceTime.CurrentSpaceTime.Width * .05f; x < SpaceTime.CurrentSpaceTime.Width * .95f; x += 7)
            {
                for (var y = SpaceTime.CurrentSpaceTime.Height * .05f; y < SpaceTime.CurrentSpaceTime.Height * .95f; y += 7)
                {
                    
                    var friendly = new Friendly()
                    {
                        Symbol = RandomLetter(),
                    };
                    var fBot = new Bot(friendly);
                    fBot.Strategy = new AvoidEnemies();
                    friendly.MoveTo(x, y);

                    SpaceTime.CurrentSpaceTime.Delay(()=> SpaceTime.CurrentSpaceTime.Add(friendly), (int)(x+y));
                }
            }
        }
    }
}
