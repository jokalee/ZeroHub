using System;
using System.Threading.Tasks;
using System.Threading;
using ZeroHub.Packets;

namespace ZeroHub
{
    public class SharpNet : IDisposable
    {
        public SharpNet() : this(new SharpTap())
        {
            
        }

        public SharpNet(SharpTap Tap)
        {
            this.Tap = Tap;
            OnReceiveIPv4Packet += ParseIPv4Packet;

            Run();
        }

        public void Dispose()
        {
            mCts.Cancel();
        }

        public readonly SharpTap Tap;
        private int mID = 0;

        public delegate void OnReceiveIPv4PacketHandler(IPv4Packet Packet);
        public event OnReceiveIPv4PacketHandler OnReceiveIPv4Packet = delegate { };

        public delegate void OnReceiveICMPPacketHandler(ICMPPacket Packet);
        public event OnReceiveICMPPacketHandler OnReceiveICMPPacket = delegate { };
        public delegate void OnReceiveUDPPacketHandler(UDPPacket Packet);
        public event OnReceiveUDPPacketHandler OnReceiveUDPPacket = delegate { };
        public delegate void OnReceiveTCPPacketHandler(TCPPacket Packet);
        public event OnReceiveTCPPacketHandler OnReceiveTCPPacket = delegate { };

        private byte[] mRxBuffer = new byte[0xFFFF];

        private CancellationTokenSource mCts;
        private SemaphoreSlim mTxSemaphore = new SemaphoreSlim(1, 1);
        
        private void Run()
        {
            mCts = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (true)
                {
                    //Tap.TapDriver.Read(mRxBuffer, 0, 0xFFFF);
                    Tap.TapDriver.ReadAsync(mRxBuffer,0xFFFF).Wait();
                    int version = mRxBuffer[0] >> 4;
                    int IHL = mRxBuffer[0] & 0x0F;
                    if (version == 4 && IHL == 5) // IPv4 Packet with no options field
                    {
                        IPv4Packet received = new IPv4Packet(mRxBuffer);
                        OnReceiveIPv4Packet(received);
                    }
                }
            }, mCts.Token);
        }

        private void ParseIPv4Packet(IPv4Packet Packet)
        {
            switch(Packet.Protocol)
            {
                case EProtocolType.UDP:
                    OnReceiveUDPPacket(new UDPPacket(Packet));
                    break;
                case EProtocolType.TCP:
                    OnReceiveTCPPacket(new TCPPacket(Packet));
                    break;
                case EProtocolType.ICMP:
                    OnReceiveICMPPacket(new ICMPPacket(Packet));
                    break;
            }
        }

        public async Task SendIPPacketAsync(IPv4Packet Packet)
        {
            Packet.Id = mID++;
            byte[] data = Packet.ToBytes();
            await mTxSemaphore.WaitAsync();
            await Tap.TapDriver.WriteAsync(data);
            //Tap.TapDriver.Flush();
            //Tap.TapDriver.InvokeMethod("FlushWrite", true);
            mTxSemaphore.Release();
        }

        public void SendIPPacket(IPv4Packet Packet)
        {
            SendIPPacketAsync(Packet).Wait();
        }
    }
}
