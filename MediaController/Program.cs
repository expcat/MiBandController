using System;
using Windows.Media.Control;
using System.Threading.Tasks;

namespace MediaController
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var sessions = (await GlobalSystemMediaTransportControlsSessionManager.RequestAsync()).GetSessions();
            foreach(var item in sessions)
            {
                var prop = await item.TryGetMediaPropertiesAsync();
                Console.WriteLine(prop.Title);
                Console.WriteLine(item.SourceAppUserModelId);
                //await item.TryTogglePlayPauseAsync();
            }
            //this project can get the control session of UWP APP

            Console.ReadKey();
        }
    }
}
