using System;
using System.Linq;
using System.Diagnostics;
using System.ServiceProcess;
using System.IO;

/// <summary>
/// Windows Bulk Crap Cleaner & Garbage Collector
/// </summary>

/*
*               GLWT(Good Luck With That) Public License
*                 Copyright(c) Everyone, except Author
*
*Everyone is permitted to copy, distribute, modify, merge, sell, publish,
*sublicense or whatever they want with this software but at their OWN RISK.
*
*                            Preamble
*
*The author has absolutely no clue what the code in this project does.
*It might just work or not, there is no third option.
*
*
*                GOOD LUCK WITH THAT PUBLIC LICENSE
*   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION, AND MODIFICATION
*
*  0. You just DO WHATEVER YOU WANT TO as long as you NEVER LEAVE A
*TRACE TO TRACK THE AUTHOR of the original product to blame for or hold
*responsible.
*
*IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
*LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
*FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
*DEALINGS IN THE SOFTWARE.
*/
namespace WBCCGC
{
    class Program
    {
        public static string[] Blacklist = new string[] //Blacklist is for killing SERVICES
        {
           "CertPropSvc","CDPSvc","CDPUserSvc","CscService","DiagTrack","DoSvc","DevicesFlow",
            "DevicesFlowUserSvc","DevQueryBroker","DsmSvc","DusmSvc","DPS","diagnosticshub.standardcollector.service",
            "dmwappushservice","MapsBroker","MessagingService","OneSyncSvc","PimIndexMaintenanceSvc","PrintWorkflowUserSvc",
            "PcaSvc","RemoteRegistry","SmsRouter","Spooler","TrkWks","StiSvc","SecurityHealthService","Sense","UnistoreSvc",
            "UserDataSvc","WpnUserService","WerSvc","WMPNetworkSvc","WSearch","WdNisDrv","WdNisSvc","WinDefend"
        };

        static Process me = Process.GetCurrentProcess();

        public static string[] Whitelist = new string[] //Whitelist is for saving PROCESSES
        {
            "winlogon","system idle process","taskmgr","spoolsv",
            "csrss","smss","svchost","services","lsass", "explorer",
            "sihost", "cmd", "dwm", "system", "fontdrv", "fontdrvhost",
            "ApplicationFrameHost"/*UWP*/,"SkypeBackgroundHost",
            "SkypeApp", "audiodg", "SearchIndexer", /*"WUDFHost", "ShellExperienceHost", "StartMenuExperienceHost",
            "ctfmon", "WmiPrvSE", */"Music.UI", "firefox", "SgrmBroker", "Memory Compression", "MsMpEng", "backgroundTaskHost", "Registry", "wininit", "Idle", "NisSrv",
            "ServiceHub.SettingsHost", "PerfWatson2","devenv", "ScriptedSandbox64",
            "VBCSCompiler","MSBuild","ServiceHub.DataWarehouseHost","ServiceHub.RoslynCodeAnalysisService32",
            "ServiceHub.ThreadedWaitDialog","ServiceHub.Host.CLR.x86","vsls-agent","conhost","ServiceHub.IdentityHost",
            "Microsoft.ServiceHub.Controller","RuntimeBroker", me.ProcessName.Replace(".exe", "")
        };





        static void Main(string[] args)
        {
            Array.Sort(Whitelist);
            //File init
            string wl = Path.Combine(Environment.CurrentDirectory, "whitelist.txt");
            string bl = Path.Combine(Environment.CurrentDirectory, "blacklist.txt");


            if (!File.Exists(wl))
                File.WriteAllLines(wl, Whitelist);
            else
                Whitelist = File.ReadAllLines(wl);

            if (!File.Exists(bl))
                File.WriteAllLines(bl, Blacklist);
            else
                Blacklist = File.ReadAllLines(bl);

            if (!Whitelist.Contains(me.ProcessName.Replace(".exe", "")))
            {
                var wll = Whitelist.ToList();
                wll.Add(me.ProcessName.Replace(".exe", ""));
                Whitelist = wll.ToArray();
            }
                

            int killed = 0;
            int saved = 0;
            int errored = 0;
            long savedmb = 0;

            foreach(Process p in Process.GetProcesses())
            {
                bool kill = true;

                foreach(string i in Whitelist)
                    if (p.ProcessName.StartsWith(i, StringComparison.CurrentCultureIgnoreCase)) { kill = false; }

                

                try
                {
                    if (kill)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"Killing {p.ProcessName}...");
                        p.Kill();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Killed {p.ProcessName} with PID {p.Id}");
                        killed++;
                        savedmb += p.PrivateMemorySize64;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Saved {p.ProcessName} with PID {p.Id}");
                        saved++;
                    }
                   
                }
                catch(Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERROR while killing {p.ProcessName} with PID {p.Id}\nERR: {e.Message}");
                    errored++;
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\n\nKilled {killed} processes, Saved {saved} processes and failed to kill {errored} processes");
            Console.WriteLine($"We cleared {savedmb / 1024 /1024} MB for you!");
            Console.ReadKey(true);
            Console.Clear();
            Console.WriteLine("Now let's fuck off the SERVICES :D");

            var s = ServiceController.GetServices();
            s = s.Where(i => i.Status == ServiceControllerStatus.Running).OrderBy(j => j.ServiceType).ToArray();

            foreach(var service in s)
            {
                bool suggested = false;

                foreach (string name in Blacklist)
                    if (service.ServiceName.StartsWith(name, StringComparison.CurrentCultureIgnoreCase))
                        suggested = true;

                if(service.CanStop && suggested)
                {
                    try
                    {
                        service.Stop();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Service {service.DisplayName} stopped");
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error stopping {service.DisplayName}, \nError: {e.Message}");
                    }
                }
                else if (service.CanStop)
                {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"{service.DisplayName} ({service.ServiceName}) can be stopped (not suggested), TYPE: {service.ServiceType} \n Do you want that? (y/n)");
                        if (Console.ReadLine().StartsWith("y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            try
                            {
                                service.Stop();
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Service {service.DisplayName} stopped");
                            }
                            catch (Exception e)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error stopping {service.DisplayName}, \nError: {e.Message}");
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Skipped {service.DisplayName}");
                        }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Skipped {service.DisplayName}");
                }

                
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
            Console.WriteLine("DONE!!!!!");
            Console.ReadKey(true);
        }
    }
}
