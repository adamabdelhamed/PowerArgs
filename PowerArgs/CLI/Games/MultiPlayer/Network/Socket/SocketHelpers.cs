using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PowerArgs.Games
{
    public static class SocketHelpers
    {
        public static Socket AcceptSocket(this TcpListener tcpListener, TimeSpan timeout, int pollInterval = 10)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.Elapsed < timeout)
            {
                if (tcpListener.Pending())
                    return tcpListener.AcceptSocket();

                Thread.Sleep(pollInterval);
            }
            throw new TimeoutException();
        }

        public static void Read(Lifetime lifetimeThatMatters, Socket socket, byte[] buffer, int expectedLength)
        {
            if (Debugger.IsAttached == false)
            {
                socket.ReceiveTimeout = 1000;
            }
            while (lifetimeThatMatters.IsExpired == false)
            {
                try
                {
                    var read = socket.Receive(buffer, 0, expectedLength, SocketFlags.None);
                    while(read < expectedLength)
                    {
                        read+= socket.Receive(buffer, read, expectedLength - read, SocketFlags.None);
                    }

                    if (read != expectedLength) throw new IOException("Did not get the right amount of bytes");
                    return;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
