using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.Media.Control;

namespace MiBandController
{
    class Program
    {
        private static string MusicName = "All Around The World";
        private static IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessions;
        private static GlobalSystemMediaTransportControlsSessionManager sessionManager;

        public static BluetoothLEAdvertisementWatcher bluetoothLEAdvertisementWatcher = new BluetoothLEAdvertisementWatcher()
        {
            ScanningMode = BluetoothLEScanningMode.Active,
        };

        public static void Cout<T>(string key,T value,bool NewLine = true)
        {
            Console.Write("["+key+"]");
            Console.Write(value);
            if (NewLine) Console.Write("\n");
        }

        public static void StartScan()
        {
            bluetoothLEAdvertisementWatcher.Received += WatcherHandle;
            bluetoothLEAdvertisementWatcher.Start();
        }

        static async Task Main(string[] args)
        {
            sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            sessionManager.SessionsChanged += SessionManager_SessionsChanged;
            sessions = sessionManager.GetSessions();
            foreach(var session in sessions)
            {
                session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                MusicName = (await session.TryGetMediaPropertiesAsync()).Title;
            }
            //Initialize the music controller  (SMTC)
            //NOTICED   IT'S ONLY SUPPORT WINDOWS10 UWP APP.    OR OTHER APP PATCHED.

            await UUID.ConnectDevice();
            Cout("ConnectStatus", UUID.BluetoothLEDevice.ConnectionStatus);
            Cout("DeviceName", UUID.BluetoothLEDevice.Name);

            await UUID.AlertService.GetService();
            await UUID.AlertService.NewAlert.GetCharacteristic();

            await UUID.DeviceInformation.GetService();
            await UUID.DeviceInformation.HardwareRevision.GetCharacteristic();
            await UUID.DeviceInformation.SerialNumber.GetCharacteristic();
            await UUID.DeviceInformation.SoftwareRevision.GetCharacteristic();

            await UUID.MiBand2Service.GetService();
            await UUID.MiBand2Service.Music.GetCharacteristic();
            await UUID.MiBand2Service.MusicNotify.GetCharacteristic();
            await UUID.MiBand2Service.Date.GetCharacteristic();
            await UUID.MiBand2Service.BatteryInfo.GetCharacteristic();

            Cout("SetMusicNotifyRep", (await UUID.MiBand2Service.MusicNotify.SetCallBack(MusicNotificationAsync)).Status);

            Cout("HardwareRevision", await UUID.DeviceInformation.HardwareRevision.GetValue());
            Cout("SerialNumber", await UUID.DeviceInformation.SerialNumber.GetValue());
            Cout("SoftwareRevision", await UUID.DeviceInformation.SoftwareRevision.GetValue());

            Cout("MiBandDate", (await UUID.MiBand2Service.Date.GetDate()).ToString());
            Cout("BatteryInfo", (await UUID.MiBand2Service.BatteryInfo.GetBatteryInfo()).ToString());

            while (false)
            {
                MusicName = Console.ReadLine();
                //Cout("WirteRep", (await UUID.MiBand2Service.Music.SetTest()).Status);
                await UUID.MiBand2Service.Music.SetMusicName(MusicName);
            }

            while (false)
            {
                byte type = Convert.ToByte(Console.ReadLine());
                string content = Console.ReadLine();
                var ret = await UUID.AlertService.NewAlert.SendAlert(type, content);
                Cout("WriteRep", ret.Status);
                //Test the Alert code
            }

            Console.ReadKey();
        }

        private static async void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            MusicName = (await sender.TryGetMediaPropertiesAsync()).Title;
            await UUID.MiBand2Service.Music.SetMusicName(MusicName);
        }

        private static void SessionManager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            sessions = sender.GetSessions();
            Cout("SMTCSessionChanged", sessions.Count);
            //I don't know if it's correctly here.
        }

        private static async void MusicNotificationAsync(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            DataReader dataReader = DataReader.FromBuffer(args.CharacteristicValue);
            dataReader.ReadByte();
            byte ret = dataReader.ReadByte();
            Cout("MusicNotification", ret);
            if(ret == 0xe0 || ret == 23 || ret == 247)
            {
                Cout("SetMusicName", MusicName);
                await UUID.MiBand2Service.Music.SetMusicName(MusicName);
            }
            //it may cause the speed of code a bit low,but i'm more lazy.
            foreach(var item in sessions)
            {
                switch (ret)
                {
                    case 0x00: await item.TryPlayAsync(); break;
                    case 0x01: await item.TryPauseAsync(); break;
                    case 0x02: await item.TryPlayAsync(); break;
                    case 0x03: await item.TrySkipNextAsync(); break;
                    case 0x04: await item.TrySkipPreviousAsync(); break;
                    case 0x05: await item.TryChangeChannelUpAsync(); break;
                    case 0x06: await item.TryChangeChannelDownAsync(); break;
                }
            }
        }

        private static void WatcherHandle(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (args.Advertisement.LocalName.IndexOf("Mi Smart Band 4") != -1)
            {
                Console.WriteLine(args.BluetoothAddress);
            }
        }
    }
}