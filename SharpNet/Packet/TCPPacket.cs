using System;
using System.Linq;

namespace ZeroHub.Packets
{
    public class TCPPacket : IPv4Packet
    {
        public TCPPacket(byte[] Data)
        {
            Header.Protocol = EProtocolType.TCP;
            this.Data = new byte[Data.Length];
            Buffer.BlockCopy(Data, 0, this.Data, 0, Data.Length);
        }
        public TCPPacket(IPv4Packet Packet) : base(Packet)
        {
            SourcePort = (payload[0] << 8) | payload[1];
            DestPort = (payload[2] << 8) | payload[3];
            SeqNumber = (payload[4] << 24) | (payload[5] << 16)
                      | (payload[6] << 8) | payload[7];
            AckNumber = (payload[8] << 24) | (payload[9] << 16)
                      | (payload[10] << 8) | payload[11];
            DataOffset = (payload[12] & 0xF0) >> 4;
            NS = (payload[12] & 0x01) == 1;
            CWR = (payload[13] & 0x80) == 1;
            ECE = (payload[13] & 0x40) == 1;
            URG = (payload[13] & 0x20) == 1;
            ACK = (payload[13] & 0x10) == 1;
            PSH = (payload[13] & 0x08) == 1;
            RST = (payload[13] & 0x04) == 1;
            SYN = (payload[13] & 0x02) == 1;
            FIN = (payload[13] & 0x01) == 1;
            WindowSize = (payload[14] << 8) | payload[15];
            Checksum = (payload[16] << 8) | payload[17];
            UrgentPointer = (payload[18] << 8) | payload[19];

            if(DataOffset > 5)
            {
                Options = new byte[(DataOffset - 5) * 4];
                Buffer.BlockCopy(payload, 20, Options, 0, (DataOffset - 5) * 4);
            }

            Data = new byte[Header.TotalLength - Header.IHL * 4 - DataOffset * 4];
            Buffer.BlockCopy(payload, DataOffset * 4, Data, 0, Header.TotalLength - Header.IHL * 4 - DataOffset * 4);
        }

        public int SourcePort;
        public int DestPort;
        public int SeqNumber;
        public int AckNumber;
        public int DataOffset;

        // Control Bits
        public bool NS;
        public bool CWR;
        public bool ECE;
        public bool URG;
        public bool ACK;
        public bool PSH;
        public bool RST;
        public bool SYN;
        public bool FIN;

        public int WindowSize;
        public int Checksum { get; private set; }
        public int UrgentPointer;

        public byte[] Options = new byte[0];

        public byte[] Data { get; private set; }

        public override byte[] ToBytes()
        {
            Checksum = 0;
            Header.TotalLength = Header.IHL * 4 + 20 +Options.Length + Data.Length;

            payload = new byte[Header.TotalLength-Header.IHL*4];
            payload[0] = (byte)(SourcePort >> 8);
            payload[1] = (byte)(SourcePort & 0xFF);
            payload[2] = (byte)(DestPort >> 8);
            payload[3] = (byte)(DestPort & 0xFF);

            payload[4] = (byte)(SeqNumber >> 24);
            payload[5] = (byte)(SeqNumber >> 16);
            payload[6] = (byte)(SeqNumber >> 8);
            payload[7] = (byte)(SeqNumber);

            payload[8] = (byte)(AckNumber >> 24);
            payload[9] = (byte)(AckNumber >> 16);
            payload[10] = (byte)(AckNumber >> 8);
            payload[11] = (byte)(AckNumber);

            payload[12] = (byte)((DataOffset << 4) | (NS?1:0));
            payload[13] = (byte)(((CWR ? 1 : 0) << 7)
                               | ((ECE ? 1 : 0) << 6)
                               | ((URG ? 1 : 0) << 5)
                               | ((ACK ? 1 : 0) << 4)
                               | ((PSH ? 1 : 0) << 3)
                               | ((RST ? 1 : 0) << 2)
                               | ((SYN ? 1 : 0) << 1)
                               | ((FIN ? 1 : 0)));
            payload[14] = (byte)(WindowSize >> 8);
            payload[15] = (byte)(WindowSize & 0xFF);
            payload[16] = (byte)(Checksum >> 8);
            payload[17] = (byte)(Checksum & 0xFF);
            payload[18] = (byte)(UrgentPointer >> 8);
            payload[19] = (byte)(UrgentPointer & 0xFF);

            Buffer.BlockCopy(Options, 0, payload, 20, Options.Length);
            Buffer.BlockCopy(Data, 0, payload, 20+ Options.Length, Data.Length );
            // cal checksum
            byte[] pseudo = new byte[12];
            int[] ip = Header.SourceIP.Split('.').Select(int.Parse).ToArray();
            pseudo[0] = (byte)ip[0];
            pseudo[1] = (byte)ip[1];
            pseudo[2] = (byte)ip[2];
            pseudo[3] = (byte)ip[3];
            ip = Header.DestIP.Split('.').Select(int.Parse).ToArray();
            pseudo[4] = (byte)ip[0];
            pseudo[5] = (byte)ip[1];
            pseudo[6] = (byte)ip[2];
            pseudo[7] = (byte)ip[3];
            pseudo[8] = 0;
            pseudo[9] = (byte)EProtocolType.TCP;
            pseudo[10] = (byte)((20+Options.Length+Data.Length) >> 8);
            pseudo[11] = (byte)((20 + Options.Length + Data.Length) & 0xFF);

            long sum = 0;
            for (int i = 0; i < 12; i += 2)
                sum += ((pseudo[i] << 8) & 0xFF00) + (pseudo[i + 1] & 0xFF);
            int remainder = payload.Length % 2;
            for (int i = 0; i < payload.Length - remainder; i += 2)
                sum += ((payload[i] << 8) & 0xFF00) + (payload[i + 1] & 0xFF);
            if (remainder != 0)
                sum += ((payload.Last() << 8) & 0xFF00) + (0 & 0xFF);
            while ((sum >> 16) != 0)
                sum = (sum & 0xFFFF) + (sum >> 16);
            Checksum = (ushort)(~sum);
            payload[16] = (byte)(Checksum >> 8);
            payload[17] = (byte)(Checksum & 0xFF);
            return base.ToBytes();
        }
    }
}
