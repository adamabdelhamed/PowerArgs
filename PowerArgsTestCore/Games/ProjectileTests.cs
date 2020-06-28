using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;
using PowerArgs.Cli.Physics;
using System.Threading.Tasks;
using PowerArgs.Games;

namespace ArgsTests.CLI.Physics
{
    [TestClass]
    [TestCategory(Categories.Physics)]
    public class ProjectileTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task TestProjectilesHitTargets() => await PhysicsTest.Test(50,25, TestContext, async (app, stPanel) =>
        {
            var player = SpaceTime.CurrentSpaceTime.Add(new MainCharacter());
            player.Velocity.Angle = 0;
            player.MoveTo(25, 12);

            for (var a = 0; a < 360; a += 5)
            {
                player.Velocity.Angle = a;
                var target = SpaceTime.CurrentSpaceTime.Add(new Character());
                var loc = player.MoveTowards(a, 12);
                target.MoveTo(loc.Left, loc.Top);
                target.AddTag("enemy");

                await Time.CurrentTime.DelayAsync(100);
                using (var testLifetime = new Lifetime())
                {
                    var succes = false;
                    Velocity.GlobalImpactOccurred.SubscribeForLifetime(i =>
                    {
                        if (succes == true) Assert.Fail("Success already happened");

                        if (i.MovingObject is Projectile && i.ObstacleHit == target)
                        {
                            succes = true;
                        }
                    }, testLifetime);

                    player.Inventory.Items.Add(new Pistol() { AmmoAmount = 1 });
                    player.Inventory.PrimaryWeapon.TryFire(false);
                    await Time.CurrentTime.DelayAsync(2000);
                    Assert.IsTrue(succes);
                    target.Lifetime.Dispose();
                }
            }
        });
    }
}
