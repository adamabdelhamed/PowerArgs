using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Games;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ArgsTests.CLI.Games
{
    [TestClass]
    [TestCategory(Categories.Games)]
    public class MultiPlayerTests
    {
        [TestMethod]
        public async Task TestDeathmatchInProc()
        {

            var serverInfo = new ServerInfo() { Port = 8080, Server = "testserver" };
            var server = new MultiPlayerServer(new InProcServerNetworkProvider(serverInfo));
            var client1 = new MultiPlayerClient(new InProcClientNetworkProvider("client1"));
            var client2 = new MultiPlayerClient(new InProcClientNetworkProvider("client2"));
            await TestDeathmatch(server, serverInfo, client1, client2, 100);
        }

        [TestMethod, Timeout(4000)]
        public async Task TestDeathmatchWithSockets()
        {
            var socketServer = new SocketServerNetworkProvider(8080);
            var server = new MultiPlayerServer(socketServer);
            var client1 = new MultiPlayerClient(new SocketClientNetworkProvider());
            var client2 = new MultiPlayerClient(new SocketClientNetworkProvider());
            await TestDeathmatch(server, socketServer.ServerInfo, client1, client2, 500);
        }

        [TestMethod]
        public async Task TestRequestResponseInProc()
        {
            var serverInfo = new ServerInfo() { Port = 8080, Server = "testserver" };
            var server = new MultiPlayerServer(new InProcServerNetworkProvider(serverInfo));
            var client = new MultiPlayerClient(new InProcClientNetworkProvider("client1"));
            await TestRequestResponse(server, serverInfo, client);
        }

        [TestMethod]
        public async Task TestRequestResponseWithSockets()
        {
            var socketServer = new SocketServerNetworkProvider(8080);
            var server = new MultiPlayerServer(socketServer);
            var client = new MultiPlayerClient(new SocketClientNetworkProvider());

             await TestRequestResponse(server, socketServer.ServerInfo, client);
             
        }

        private async Task TestDeathmatch(MultiPlayerServer server, ServerInfo serverInfo, MultiPlayerClient client1, MultiPlayerClient client2, int delayMs)
        {
            Exception undeliverableException = null;
            Exception deathmatchException = null;

            server.Undeliverable.SubscribeForLifetime((args) => undeliverableException = args.Exception, server);

            var deathmatch = new Deathmatch(new MultiPlayerContestOptions()
            {
                MaxPlayers = 2,
                Server = server
            });

            deathmatch.OrchestrationFailed.SubscribeOnce((ex) => deathmatchException = ex);

            // the game starts
            await deathmatch.Start();

            // both clients start waiting for the start of the game
            var client1StartTask = client1.EventRouter.GetAwaitable<StartGameMessage>();
            var client2StartTask = client2.EventRouter.GetAwaitable<StartGameMessage>();
            
            // both clients get notified of each other's presence
            var client1SeesClient2Task = client1.EventRouter.GetAwaitable<NewUserMessage>();
            var client2SeesClient1Task = client2.EventRouter.GetAwaitable<NewUserMessage>();

            // both clients connect, which should trigger the start of the game
            await client1.Connect(serverInfo).AsAwaitable();
            Console.WriteLine("client 1 connected");
            await client2.Connect(serverInfo).AsAwaitable();
            Console.WriteLine("client 2 connected");

            // make sure both clients got the start event
            await client1StartTask;
            await client2StartTask;

            await client1SeesClient2Task;
            await client2SeesClient1Task;

            Assert.AreEqual(client2.ClientId, client1SeesClient2Task.Result.NewUserId);
            Assert.AreEqual(client1.ClientId, client2SeesClient1Task.Result.NewUserId);

            var client1GameOverTask = client1.EventRouter.GetAwaitable<GameOverMessage>();
            var client2GameOverTask = client2.EventRouter.GetAwaitable<GameOverMessage>();

            var response = await client1.SendRequest(new DamageMessage()
            {
                DamagedClient = client2.ClientId,
                NewHP = 0
            }, timeout: TimeSpan.FromDays(1)).AsAwaitable();
 
            // make sure both clients got the game over event event
            await Task.WhenAll(client1GameOverTask, client2GameOverTask);
            Assert.AreEqual(client1.ClientId, client1GameOverTask.Result.WinnerId);
            Assert.AreEqual(client1.ClientId, client2GameOverTask.Result.WinnerId);

            client1.Dispose();
            client2.Dispose();
            server.Dispose();
            Assert.IsTrue(deathmatch.IsExpired);
            Assert.IsNull(undeliverableException);
            Assert.IsNull(deathmatchException);
        }

        private async Task TestRequestResponse(MultiPlayerServer server, ServerInfo serverInfo, MultiPlayerClient client)
        {
            await server.OpenForNewConnections().AsAwaitable();
            Console.WriteLine("server is listening");
            await client.Connect(serverInfo).AsAwaitable();


            try
            {
                var sw = Stopwatch.StartNew();
                var response = await client.SendRequest(new PingMessage(), timeout: TimeSpan.FromDays(1)).AsAwaitable();
                sw.Stop();
                Console.WriteLine("ping took " + sw.ElapsedMilliseconds + " ms");
            }
            catch (Exception ex)
            {
                throw;
            }

         

            try
            {
                await client.SendRequest(new PingMessage() { Delay = 300 }, timeout: TimeSpan.FromSeconds(.1)).AsAwaitable();
                Assert.Fail("A timeout exception should have been thrown");
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
