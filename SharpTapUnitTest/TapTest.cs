using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroHub
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class TapTest
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            tap = new SharpTap();
            profile = TapProfile.Default;
        }

        private static SharpTap tap;
        private static TapProfile profile;

        public static NetworkInterface[] GetAdapters()
        {
            var matches = NetworkInterface.GetAllNetworkInterfaces().Where(
                nic => nic.Description.StartsWith(profile.TapDescription));
            if (matches.Count() == 0)
                return new NetworkInterface[0];
            else
                return matches.ToArray();
        }

        [TestMethod]
        public void TestGet()
        {
            string mac = BitConverter.ToString(tap.Mac).Replace("-", string.Empty);
            NetworkInterface adapter = tap.Adapter;

            Assert.IsTrue(adapter != null,"Get mac address failed");

            int[] version = profile.Version;          
            Assert.AreEqual(profile.Version[0], version[0], "Major version does not match");
            Assert.AreEqual(profile.Version[1], version[1], "Minor version does not match");
            Assert.AreEqual(profile.Version[2], version[2], "Debug version does not match");

            Assert.AreEqual(tap.MTU, adapter.GetIPProperties().GetIPv4Properties().Mtu, "Mtu does not match");
        }

        [TestMethod]
        public void TestSetMediaStatus()
        {
            tap.SetMediaStatus(true);
            NetworkInterface adapter = tap.Adapter;// refresh status
            Assert.AreNotEqual(OperationalStatus.Down, adapter.OperationalStatus, "Set media status up failed");

            tap.SetMediaStatus(false);         
            adapter = tap.Adapter;// refresh status
            Assert.AreEqual(OperationalStatus.Down, adapter.OperationalStatus, "Set media status down failed");
        }
    }
}
