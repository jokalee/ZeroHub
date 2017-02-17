using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroHub
{
    public class TapProfile
    {
        public string UsermodeDeviceSpace = "\\\\.\\Global\\";
        public string AdapterKey = "SYSTEM\\CurrentControlSet\\Control\\Class\\{4D36E972-E325-11CE-BFC1-08002BE10318}";
        public string ConnectionKey = "SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}";
        public string TapName = "TAP0901";

        public string DriverPath = AppDomain.CurrentDomain.BaseDirectory + "\\Drivers";

        public string TapDescription = "TAP-Windows Adapter V9";

        public int[] Version = { 9, 21, 0 };

        public string DeviceGUID
        {
            get
            {
                RegistryKey regAdapters = Registry.LocalMachine.OpenSubKey(AdapterKey, false);
                string[] keyNames = regAdapters.GetSubKeyNames();
                string devGuid = "";
                foreach (string x in keyNames)
                {
                    RegistryKey regAdapter = regAdapters.OpenSubKey(x, false);
                    object id = regAdapter.GetValue("ComponentId");
                    if (id != null && id.ToString().ToUpper() == TapName)
                        return regAdapter.GetValue("NetCfgInstanceId").ToString();
                }
                return devGuid;
            }
        }

        public string DeviceName
        {
            get
            {
                if (DeviceGUID != "")
                {
                    RegistryKey regConnection = Registry.LocalMachine.OpenSubKey(ConnectionKey + "\\" + DeviceGUID + "\\Connection", true);
                    object id = regConnection.GetValue("Name");
                    if (id != null) return id.ToString();
                }
                return "";
            }
        }

        public bool IsInstalled
        {
            get
            {
                return DeviceGUID != "";
            }
        }

        public static TapProfile Default
        {
            get { return new TapProfile(); }
        }

    }
}
