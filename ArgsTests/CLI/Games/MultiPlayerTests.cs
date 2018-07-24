using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Games;

namespace ArgsTests.CLI.Games
{
    [TestClass]
    public class MultiPlayerTests
    {
        [TestMethod]
        public async Task TestDeathmatchInProc()
        {
            var server = new MultiPlayerServer(new InProcServerNetworkProvider("testserver"));
            var client1 = new MultiPlayerClient(new InProcClientNetworkProvider("client1"));
            var client2 = new MultiPlayerClient(new InProcClientNetworkProvider("client2"));
            await TestDeathmatch(server, client1, client2, 100);
        }

        [TestMethod, Timeout(4000)]
        public async Task TestDeathmatchWithSockets()
        {
            var server = new MultiPlayerServer(new SocketServerNetworkProvider(8080));
            var client1 = new MultiPlayerClient(new SocketClientNetworkProvider());
            var client2 = new MultiPlayerClient(new SocketClientNetworkProvider());
            await TestDeathmatch(server, client1, client2, 500);
        }

        [TestMethod]
        public async Task TestRequestResponseInProc()
        {
            var server = new MultiPlayerServer(new InProcServerNetworkProvider("testserver"));
            var client = new MultiPlayerClient(new InProcClientNetworkProvider("client1"));
            await TestRequestResponse(server, client);
        }

        [TestMethod]
        public async Task TestRequestResponseWithSockets()
        {
            var server = new MultiPlayerServer(new SocketServerNetworkProvider(8080));
            var client = new MultiPlayerClient(new SocketClientNetworkProvider());

             await TestRequestResponse(server, client);
             
        }

        private async Task TestDeathmatch(MultiPlayerServer server, MultiPlayerClient client1, MultiPlayerClient client2, int delayMs)
        {
            server.Undeliverable.SubscribeForLifetime((args) =>
            {
                Assert.Fail("There was an undeliverable message");
            }, server);

            var deathmatch = new Deathmatch(new MultiPlayerContestOptions()
            {
                MaxPlayers = 2,
                Server = server
            });

            // the game starts
            deathmatch.Start();
            await Task.Delay(delayMs);
            // both clients start waiting for the start of the game
            var client1StartTask = client1.EventRouter.Await("start/{*}");
            var client2StartTask = client2.EventRouter.Await("start/{*}");

            var client1SeesClient2Task = client1.EventRouter.Await("newuser/{*}");
            var client2SeesClient1Task = client2.EventRouter.Await("newuser/{*}");

            // both clients connect, which should trigger the start of the game
            await client1.Connect(server.ServerId).AsAwaitable();
            Console.WriteLine("client 1 connected");
            await client2.Connect(server.ServerId).AsAwaitable();
            Console.WriteLine("client 2 connected");

            // make sure both clients got the start event
            await client1StartTask;
            await client2StartTask;

            await client1SeesClient2Task;
            await client2SeesClient1Task;

            Assert.AreEqual(client2.ClientId, client1SeesClient2Task.Result.Data.Data["ClientId"]);
            Assert.AreEqual(client1.ClientId, client2SeesClient1Task.Result.Data.Data["ClientId"]);

            var client1GameOverTask = client1.EventRouter.Await("gameover/{*}");
            var client2GameOverTask = client2.EventRouter.Await("gameover/{*}");

            // player one sends enough damage to client 2 to win the game
            for (var i = 0; i < 10; i++)
            {
                var response = await client1.SendRequest(MultiPlayerMessage.Create(client1.ClientId, client2.ClientId, "damage", new System.Collections.Generic.Dictionary<string, string>()
                {
                    { "OpponentId", client2.ClientId }
                })).AsAwaitable();
                Assert.AreEqual("true", response.Data["accepted"]);
            }

            // make sure both clients got the game over event event
            await Task.WhenAll(client1GameOverTask, client2GameOverTask);
            Assert.AreEqual(client1.ClientId, client1GameOverTask.Result.Data.Data["winner"]);
            Assert.AreEqual(client1.ClientId, client2GameOverTask.Result.Data.Data["winner"]);
        }

        private async Task TestRequestResponse(MultiPlayerServer server, MultiPlayerClient client)
        {
            await server.OpenForNewConnections().AsAwaitable();
            Console.WriteLine("server is listening");
            await client.Connect(server.ServerId).AsAwaitable();


            try
            {
                var sw = Stopwatch.StartNew();
                var response = await client.SendRequest(MultiPlayerMessage.Create(client.ClientId, server.ServerId, "ping")).AsAwaitable();
                sw.Stop();
                Console.WriteLine("ping took " + sw.ElapsedMilliseconds + " ms");
            }
            catch (Exception ex)
            {
                throw;
            }

            try
            {
                await client.SendRequest(MultiPlayerMessage.Create(client.ClientId, server.ServerId, "Hello")).AsAwaitable();
                Assert.Fail("An exception should have been thrown");
            }
            catch (PromiseWaitException ex)
            {
                Assert.AreEqual(1, ex.InnerExceptions.Count);
                Assert.IsTrue(ex.InnerException is IOException);
                Assert.AreEqual("NotFound", ex.InnerException.Message);
            }

            try
            {
                await client.SendRequest(MultiPlayerMessage.Create(client.ClientId, server.ServerId, "ping", new System.Collections.Generic.Dictionary<string, string>()
                {
                    { "delay", "200" }
                }), timeout: TimeSpan.FromSeconds(.1)).AsAwaitable();
                Assert.Fail("An exception should have been thrown");
            }
            catch (PromiseWaitException ex)
            {
                Assert.AreEqual(1, ex.InnerExceptions.Count);
                Assert.IsTrue(ex.InnerException is TimeoutException);
            }

            client.Dispose();
            server.Dispose();
        }
    }
}
