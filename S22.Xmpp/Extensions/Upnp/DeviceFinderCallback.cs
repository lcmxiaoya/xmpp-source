namespace S22.Xmpp.Extensions.Upnp
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UPNPLib;

    internal class DeviceFinderCallback : IUPnPDeviceFinderCallback
    {
        private ISet<UPnPDevice> devices = new HashSet<UPnPDevice>();
        private ManualResetEvent searchCompleted = new ManualResetEvent(false);

        public void DeviceAdded(int lFindData, UPnPDevice pDevice)
        {
            this.devices.Add(pDevice);
        }

        public void DeviceRemoved(int lFindData, string bstrUDN)
        {
        }

        public void SearchComplete(int lFindData)
        {
            this.searchCompleted.Set();
        }

        public IEnumerable<UPnPDevice> Devices
        {
            get
            {
                return this.devices;
            }
        }

        public WaitHandle SearchCompleted
        {
            get
            {
                return this.searchCompleted;
            }
        }
    }
}

