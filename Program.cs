using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace PockerPicker
{
    internal class Program
    {
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public const int INTERNET_OPTION_REFRESH = 37;
        static bool settingsReturn, refreshReturn;

        static string m_usage = @$"Usage : PocketPicker ""Template.json path"" ""save path""{Environment.NewLine}Powered by Sean Liu";

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

#if DEBUG
            const string debug_rules = @"C:\RPA\template.json";
            const string debug_savepath = @"C:\RPA\Catcher";
            args = new string[] { debug_rules, debug_savepath };
#endif
            if (args.Length == 0)
            {
                Console.WriteLine(m_usage);
                _quitEvent.WaitOne();
                return;
            }

            string rulesPath = args[0];
            string savePath = args[1];

            //Registe a handler that clean system proxy when app exit
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            var rules = RulesReader.GetDictFromFile(rulesPath);
            SaveFile.SavePath = savePath;

            TitanWebProxyUtility proxy = new TitanWebProxyUtility(rules);

            Console.WriteLine("Start..");
            Thread t = new(proxy.ProxyStart)
            {
                IsBackground = true
            };
            t.Start();

            _quitEvent.WaitOne();
        }

        [SupportedOSPlatform("windows")]
        static void OnProcessExit (object sender, EventArgs e)
        {
            try
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true) ?? throw new ArgumentNullException(Registry.CurrentUser.Name, nameof(Registry.CurrentUser));
                registry.SetValue("ProxyEnable", 0);
                //registry.SetValue("ProxyServer", YOURPROXY);

                // These lines implement the Interface in the beginning of program 
                // They cause the OS to refresh the settings, causing IP to realy update
                settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
                Console.WriteLine("Good Bye");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.Message}{Environment.NewLine}Please clean system proxy setting manually.");
            }
        }
    }
}