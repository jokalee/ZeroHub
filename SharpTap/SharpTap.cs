using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.NetworkInformation;

namespace ZeroHub
{
    public class SharpTap
    {
        public SharpTap() : this(TapProfile.Default)
        {

        }
        public SharpTap(TapProfile Profile)
        {
            this.Profile = Profile;
            if (!this.Profile.IsInstalled)
                throw new Exception("Tuntap driver not installed");
            mTapHandle = Win32Native.CreateFile(this.Profile.UsermodeDeviceSpace + this.Profile.DeviceGUID + ".tap", FileAccess.ReadWrite,
                FileShare.ReadWrite, 0, FileMode.Open, 0x4| 0x40000000, IntPtr.Zero); // FILE_ATTRIBUTE_SYSTEM | FILE_FLAG_OVERLAPPED 0x40000000 |FILE_FLAG_WRITE_THROUGH 0x80000000
            // rewrite a filestream for async read write with overlap, seprate read write buffer size
            //TapDriver = new FileStream(new SafeFileHandle(mTapHandle, true), FileAccess.ReadWrite, 0xFFFF, true);

            TapDriver = new TapStream(mTapHandle);

            //string path = this.Profile.UsermodeDeviceSpace + this.Profile.DeviceGUID + ".tap";
            //TapDriver = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 8, FileOptions.WriteThrough);
        }      

        public void Dispose()
        {
            TapDriver.Dispose();
        }

        public NetworkInterface Adapter
        {
            get
            {
                string mac = BitConverter.ToString(Mac).Replace("-", string.Empty);
                var matches = NetworkInterface.GetAllNetworkInterfaces().Where(
                nic => nic.GetPhysicalAddress().ToString() == mac);
                if (matches.Count() == 0)
                    return null;
                else
                    return matches.First();
            }
        }
        private static class TapWinIOControl
        {
            public static uint GetMac { get { return TapWinControlCode(1); } }
            public static uint GetVersion { get { return TapWinControlCode(2); } }
            public static uint GetMTU { get { return TapWinControlCode(3); } }
            public static uint GetInfo { get { return TapWinControlCode(4); } }
            public static uint ConfigPointToPoint { get { return TapWinControlCode(5); } }
            public static uint SetMediaStatus { get { return TapWinControlCode(6); } }
            public static uint ConfigDhcpMasq { get { return TapWinControlCode(7); } }
            public static uint GetLogLine { get { return TapWinControlCode(8); } }
            public static uint ConfigDhcpSetOpt { get { return TapWinControlCode(9); } }
            public static uint ConfigTun { get { return TapWinControlCode(10); } }
            private static uint TapWinControlCode(uint Request, uint Method = 0) //METHOD_BUFFERED
            {
                uint DeviceType = 0x00000022;//FILE_DEVICE_UNKNOWN
                uint Function = Request;
                uint Access = 0; //FILE_ANY_ACCESS
                return ((DeviceType << 16) | (Access << 14) | (Function << 2) | Method);
            }
        }

        public readonly TapProfile Profile;

        private IntPtr mTapHandle;
        public readonly TapStream TapDriver;

        public string Ip
        {
            get
            {
                return (from ip in Adapter.GetIPProperties().UnicastAddresses
                        where ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                        select ip).First().Address.ToString();
            }
        }
        public byte[] Mac
        {
            get
            {
                int len;
                byte[] mac = new byte[6];
                IntPtr ptr = Marshal.AllocHGlobal(6);
                Win32Native.DeviceIoControl(mTapHandle, TapWinIOControl.GetMac, ptr, 6, ptr, 6, out len, IntPtr.Zero);
                for (int i = 0; i < 6; i++)
                    mac[i] = Marshal.ReadByte(ptr, i);
                Marshal.FreeHGlobal(ptr);
                return mac;
            }
        }

        public int[] Version
        {
            get
            {
                int len;
                int[] version = new int[3];
                IntPtr ptr = Marshal.AllocHGlobal(12);
                Win32Native.DeviceIoControl(mTapHandle, TapWinIOControl.GetVersion, ptr, 12, ptr, 12, out len, IntPtr.Zero);
                for (int i = 0; i < 3; i++)
                    version[i] = Marshal.ReadInt32(ptr, i * 4);
                Marshal.FreeHGlobal(ptr);
                return version;
            }
        }

        public int MTU
        {
            get
            {
                int len;
                int mtu;
                IntPtr ptr = Marshal.AllocHGlobal(4);
                Win32Native.DeviceIoControl(mTapHandle, TapWinIOControl.GetMTU, ptr, 4, ptr, 4, out len, IntPtr.Zero);
                mtu = Marshal.ReadInt32(ptr);
                Marshal.FreeHGlobal(ptr);
                return mtu;
            }
        }      

        public bool ConfigTun(string LocalIP,string RemoteIP,string RemoteMask)
        {
            int len;
            IntPtr ptr = Marshal.AllocHGlobal(12);
            Marshal.WriteInt32(ptr, 0, ParseIP(LocalIP));
            Marshal.WriteInt32(ptr, 4, ParseIP(RemoteIP)); 
            Marshal.WriteInt32(ptr, 8, ParseIP(RemoteMask));
            bool result = Win32Native.DeviceIoControl(mTapHandle, TapWinIOControl.ConfigTun, ptr, 12, ptr, 12, out len, IntPtr.Zero);
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        public bool ConfigPointToPoint(string LocalIP, string RemoteIP)
        {
            int len;
            IntPtr ptr = Marshal.AllocHGlobal(8);
            Marshal.WriteInt32(ptr, 0, ParseIP(LocalIP));
            Marshal.WriteInt32(ptr, 4, ParseIP(RemoteIP));
            bool result = Win32Native.DeviceIoControl(mTapHandle, TapWinIOControl.ConfigPointToPoint, ptr, 8, ptr, 8, out len, IntPtr.Zero);
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        public bool ConfigDHCPMASQ(string DHCPIP, string DHCPMask, string DHCPMASQIP, int LeaseTimeSeconds)
        {
            int len;
            IntPtr ptr = Marshal.AllocHGlobal(16);
            Marshal.WriteInt32(ptr, 0, ParseIP(DHCPIP));
            Marshal.WriteInt32(ptr, 4, ParseIP(DHCPMask));
            Marshal.WriteInt32(ptr, 8, ParseIP(DHCPMASQIP));
            Marshal.WriteInt32(ptr, 12, LeaseTimeSeconds);
            bool result = Win32Native.DeviceIoControl(mTapHandle, TapWinIOControl.ConfigDhcpMasq, ptr, 16, ptr, 16, out len, IntPtr.Zero);
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        public bool ConfigDhcpSetOpt(byte[] Opts)
        {
            int len;
            IntPtr ptr = Marshal.AllocHGlobal(Opts.Length);
            Marshal.Copy(Opts, 0, ptr, Opts.Length);
            bool result = Win32Native.DeviceIoControl(mTapHandle, TapWinIOControl.ConfigDhcpSetOpt, ptr, (uint)Opts.Length, ptr, (uint)Opts.Length, out len, IntPtr.Zero);
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        public string Info
        {
            get
            {
                return "Not implemented by TunTap driver";
            }
        }

        public bool SetMediaStatus(bool Connected)
        {
            int len;
            IntPtr ptr = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(ptr, Connected ? 1 : 0);
            bool result = Win32Native.DeviceIoControl(mTapHandle, TapWinIOControl.SetMediaStatus, ptr, 4, ptr, 4, out len, IntPtr.Zero);
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        public void SetIpAddress(string LocalIP, string LocalMask, string Gateway)
        {
            int Idx = Adapter.GetIPProperties().GetIPv4Properties().Index;
            string command = String.Format("netsh interface ip set address {0} static {1} {2} {3}", Idx, LocalIP, LocalMask, Gateway);
            //string command = String.Format("netsh interface ip set address {0} static {1} {2}", Idx, LocalIP, LocalMask);
            Helper.Cmd.ExecuteAdmin(command);
        }

        public static int ParseIP(string IP)
        {
            byte[] address = IPAddress.Parse(IP).GetAddressBytes();
            return unchecked( (int)(address[0] 
                                  | address[1] << 8 
                                  | address[2] << 16 
                                  | address[3] << 24));
        }
    }

    //public static class FileStreamExtension
    //{
    //    public static object InvokeMethod<T>(this T obj, string methodName, params object[] args)
    //    {
    //        var type = typeof(T);
    //        var method = type.GetTypeInfo().GetDeclaredMethod(methodName);
    //        return method.Invoke(obj, args);
    //    }
    //}
}
