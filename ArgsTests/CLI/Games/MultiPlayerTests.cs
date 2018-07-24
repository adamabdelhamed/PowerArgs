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
        public async Task TestMultiPlayerProtocolInProc()
        {
            var server = new MultiPlayerServer(new InProcServerNetworkProvider("testserver"));
            var client1 = new MultiPlayerClient(new InProcClientNetworkProvider("client1"));
            var client2 = new MultiPlayerClient(new InProcClientNetworkProvider("client2"));
            await TestMultiPlayerNetwork(server, client1, client2, 0);
        }

        [TestMethod, Timeout(4000)]
        public async Task TestMultiPlayerProtocolWithSockets()
        {
            var server = new MultiPlayerServer(new SocketServerNetworkProvider(8080));
            var client1 = new MultiPlayerClient(new SocketClientNetworkProvider());
            var client2 = new MultiPlayerClient(new SocketClientNetworkProvider());
            await TestMultiPlayerNetwork(server, client1, client2, 500);
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

        private async Task TestMultiPlayerNetwork(MultiPlayerServer server, MultiPlayerClient client1, MultiPlayerClient client2, int delayMs)
        {
            var assertionCount = 0;

            server.Undeliverable.SubscribeForLifetime((args) => Assert.Fail("There was an undeliverable message"), server);

            client1.NewRemoteUser.SubscribeOnce((c) =>
            {
                Assert.AreEqual(client2.ClientId, c.ClientId);
                assertionCount++;
                Console.WriteLine("client1 received new user notification");
            });

            client2.NewRemoteUser.SubscribeOnce((c) =>
            {
                Assert.AreEqual(client1.ClientId, c.ClientId);
                assertionCount++;
                Console.WriteLine("client2 received new user notification");
            });

            await server.OpenForNewConnections().AsAwaitable();
            Console.WriteLine("server is listening");
            await client1.Connect(server.ServerId).AsAwaitable();
            Console.WriteLine("client 1 connected");
            await client2.Connect(server.ServerId).AsAwaitable();
            Console.WriteLine("client 2 connected");
            await server.CloseForNewConnections().AsAwaitable();
            Console.WriteLine("server stopped listening");
            Thread.Sleep(delayMs);
            Assert.AreEqual(2, assertionCount);

            client1.MessageReceived.SubscribeOnce((message) =>
            {
                Assert.AreEqual("HelloWorld2", message.EventId);
                assertionCount++;
                Console.WriteLine("client1 received data message");
            });

            client2.MessageReceived.SubscribeOnce((message) =>
            {
                Assert.AreEqual("HelloWorld1", message.EventId);
                assertionCount++;
                Console.WriteLine("client2 received data message");
            });

            client1.SendMessage(MultiPlayerMessage.Create(client1.ClientId, client2.ClientId, "HelloWorld1"));
            Console.WriteLine("client1 sent data message");
            Thread.Sleep(delayMs);
            client2.SendMessage(MultiPlayerMessage.Create(client2.ClientId, client1.ClientId, "HelloWorld2"));
            Console.WriteLine("client2 sent data message");
            Thread.Sleep(delayMs);
            Assert.AreEqual(4, assertionCount);

            client2.RemoteUserLeft.SubscribeOnce((c) =>
            {
                Assert.AreEqual(client1.ClientId, c.ClientId);
                assertionCount++;
                Console.WriteLine("Client1 was notified that client2 left");
            });

            client1.Dispose();
            Thread.Sleep(delayMs);
            Assert.AreEqual(5, assertionCount);

            server.Dispose();
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
                Assert.AreEqual("NoContest", ex.InnerException.Message);
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
