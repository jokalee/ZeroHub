using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroHub.Packets;

namespace ZeroHub
{
    public struct ClientProfile
    {
        public string ServerIP;
        public int ServerPort;
        public string ClientIP;
        public int ClientPort;
    }
    public class Client
    {
        public Client(SharpNet NetworkInterface,ClientProfile Profile)
        {
            mNet = NetworkInterface;
            mProfile = Profile;
            mNet.OnReceiveUDPPacket += ClientHandlerUDP;
            mNet.OnReceiveTCPPacket += ClientHandlerTCP;
        }

        private async void ClientHandlerUDP(UDPPacket Packet)
        {
            if(Packet.DestIP=="" && Packet.DestPort == 0)
            {
                byte[] buffer = Encapsulate(Packet);
                await mClient.SendAsync(buffer,buffer.Length);
            }
        }
        private async void ClientHandlerTCP(TCPPacket Packet)
        {
            if (Packet.DestIP == "" && Packet.DestPort == 0)
            {
                byte[] buffer = Encapsulate(Packet);
                await mClient.SendAsync(buffer, buffer.Length);
            }
        }

        private byte[] Encapsulate(UDPPacket Packet)
        {
            byte[] buffer = new byte[Packet.Data.Length + 123];
            //add cookie
            return buffer;
        }

        private byte[] Encapsulate(TCPPacket Packet)
        {
            byte[] buffer = new byte[Packet.Data.Length + 123];
            //add cookie
            return buffer;
        }

        public void Connect()
        {

        }

        private SharpNet mNet;
        private System.Net.Sockets.UdpClient mClient;
        private ClientProfile mProfile;
    }
}
