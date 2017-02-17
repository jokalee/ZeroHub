using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ZeroHub.Packets;

namespace ZeroHub
{
    
    public class UdpClient
    {
        public UdpClient(SharpNet NetworkInterface,int LocalPort,string FakeRemoteIP,int FakeRemotePort)
        {
            net = NetworkInterface;
            LocalIP = NetworkInterface.Tap.Ip;
            this.LocalPort = LocalPort;
            this.FakeRemoteIP = FakeRemoteIP;
            this.FakeRemotePort = FakeRemotePort;
            mRx = new BlockingCollection<UDPPacket>(new ConcurrentQueue<UDPPacket>());
            net.OnReceiveUDPPacket += Parse;
        }

        public void Dispose()
        {
            net.OnReceiveUDPPacket -= Parse;
        }

        private SharpNet net;

        private string LocalIP;
        public int LocalPort { get; private set; }
        public int FakeRemotePort { get; private set; }
        public string FakeRemoteIP { get; private set; }

        private BlockingCollection<UDPPacket> mRx;

        private void Parse(UDPPacket Packet)
        {
            if (Packet.DestPort != FakeRemotePort || Packet.DestIP != FakeRemoteIP)
                return;
            mRx.Add(Packet);
        }

        public byte[] Receive()
        {
            UDPPacket r = mRx.Take();
            return r.Data;
        }

        public Task<byte[]> ReceiveAsync()
        {
            return Task.Run(() => { UDPPacket r = mRx.Take(); return r.Data; });
        }

        public void Send(byte[] Data)
        {
            UDPPacket Packet = new UDPPacket(Data);
            Packet.DestIP = LocalIP;
            Packet.DestPort = LocalPort;
            Packet.SourceIP = FakeRemoteIP;
            Packet.SourcePort = FakeRemotePort;
            net.SendIPPacket(Packet);
        }
  
        public Task SendAsync(byte[] Data)
        {
            UDPPacket Packet = new UDPPacket(Data);
            Packet.DestIP = LocalIP;
            Packet.DestPort = LocalPort;
            Packet.SourceIP = FakeRemoteIP;
            Packet.SourcePort = FakeRemotePort;
            return net.SendIPPacketAsync(Packet);
        } 
    }
    
}
