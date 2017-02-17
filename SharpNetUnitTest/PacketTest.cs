using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using ZeroHub.Packets;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics.CodeAnalysis;

namespace ZeroHub
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class PacketTest
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            //Helper.Driver.Profile.DriverPath = "E:\\Workbench\\ZeroHub\\ZeroHub\\SharpNetUnitTest\\bin\\Debug\\Drivers\\";
            //Helper.Driver.Uninstall();
            //Helper.Driver.Install();
            LocalIP = "10.3.0.1";
            RemoteIPRange = "10.3.0.0"; // RemoteIPRange & Mask , and filter away local
            RemoteIP = "10.3.0.5";
            Mask = "255.255.255.0";
            
            net = new SharpNet();
            net.Tap.SetMediaStatus(true);
            net.Tap.ConfigTun(LocalIP, RemoteIPRange, Mask);
            net.Tap.SetIpAddress(LocalIP, Mask, "10.3.0.0");
            
            //net.Tap.ConfigDHCPMASQ("192.168.137.1", "255.255.255.255", "1.1.1.1", 2);
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();

            ping = new Ping();
            localPort = 233;
            remotePort = 233;

            local = new IPEndPoint(IPAddress.Parse(LocalIP), localPort);
            remote = new IPEndPoint(IPAddress.Parse(RemoteIP), remotePort);
            //remote = new IPEndPoint(IPAddress.Any, remotePort);
            client = new System.Net.Sockets.UdpClient(local);
        }

        [ClassCleanup]
        public static void Dispose()
        {
            client.Close();
            net.Dispose();
        }

        private static SharpNet net;
        private static string LocalIP;
        private static string RemoteIPRange;
        private static string RemoteIP;
        private static string Mask;

        private static Ping ping;

        private static int localPort;
        private static int remotePort;
        private static IPEndPoint local;
        private static IPEndPoint remote;
        private static System.Net.Sockets.UdpClient client;

        [TestMethod]
        public void TestICMPPacket()
        {
            SharpNet.OnReceiveICMPPacketHandler Handler = async (ICMPPacket Packet) =>
            {
                ICMPPacket echo = new ICMPPacket(Packet.Data);
                echo.Type = 0;
                echo.PingId = Packet.PingId;
                echo.PingSeq = Packet.PingSeq;
                echo.DestIP = Packet.SourceIP;
                echo.SourceIP = Packet.DestIP;
                await net.SendIPPacketAsync(echo);
            };
            net.OnReceiveICMPPacket += Handler;
            //Task.Delay(30000).Wait();
            for (int i = 0; i < 10; i++)
            {
                var result = ping.Send(RemoteIP, 100);
                Assert.AreEqual(IPStatus.Success, result.Status, "Timeout occured");
                Trace.WriteLine(string.Format("{0} ms delay", result.RoundtripTime));
            }
            net.OnReceiveICMPPacket -= Handler;
        }

        [TestMethod]
        public void TestUDPPacket()
        {
            SharpNet.OnReceiveUDPPacketHandler Handler = async (UDPPacket Packet) =>
            {
                if (Packet.DestPort == remotePort)
                {
                    UDPPacket echo = new UDPPacket(new byte[] { 1, 2, 3, 4, 5 });
                    echo.SourceIP = Packet.DestIP;
                    echo.DestIP = Packet.SourceIP;
                    echo.SourcePort = Packet.DestPort;
                    echo.DestPort = Packet.SourcePort;
                    await net.SendIPPacketAsync(echo);
                }
            };
            net.OnReceiveUDPPacket += Handler;

            CancellationTokenSource cts = new CancellationTokenSource();
            int tx = 0;
            int rx = 0;
            Task.Run(() =>
            {
                while (true)
                {
                    //byte[] data = client.Receive(ref remote);
                    byte[] data = client.ReceiveAsync().Result.Buffer;
                    foreach (var d in data)
                        Trace.Write(d);
                    rx++;
                }
            }, cts.Token);

            Task.Run(() =>
            {
                while (true)
                {
                    client.Send(new byte[] { 1, 2, 3, 4 }, 4, remote);
                    tx++;
                    Task.Delay(500).Wait();
                }
            }, cts.Token);
            Task.Delay(5000).Wait();
            cts.Cancel();
            net.OnReceiveUDPPacket -= Handler;
            Assert.IsTrue((rx == tx) && (rx != 0), "Send echo back received number does not match"); 
            
        }

        [TestMethod]
        public void TestUDPClient()
        {
            // turn off firewall or send something out first to allow incoming data
            UdpClient u = new UdpClient(net, localPort, RemoteIP, remotePort);
            CancellationTokenSource cts = new CancellationTokenSource();
            int tx = 0;
            int rx = 0;
            Task.Run(() =>
            {
                while (true)
                {
                    byte[] data = client.ReceiveAsync().Result.Buffer;
                    foreach (var d in data)
                        Trace.Write(d);
                    rx++;
                }
            }, cts.Token);

            Task.Run(() =>
            {
                while (true)
                {
                    client.Send(new byte[] { 1, 2, 3, 4 }, 4, remote);
                    tx++;
                    Task.Delay(500).Wait();
                }
            }, cts.Token);
            for (int i = 0; i < 20; i++)
            {
                u.Send(u.Receive());
            }
            cts.Cancel();
            u.Dispose();
            Assert.IsTrue((rx == tx) && (rx != 0), "Send echo back received number does not match");
        }

    }
}
