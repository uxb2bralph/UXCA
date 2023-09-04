using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Core.Utility;
using CommonLib.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using TWCACAPIAdapter.Properties;
using Microsoft.Extensions.Primitives;
using TWCACAPIAdapter.Helper;
using TWCACAPIAdapter.Models.ViewModel;
using System.Runtime.InteropServices;

namespace TWCACAPIAdapter
{
    public class Program
    {
        public static void Main(string[]? args)
        {
            Console.WriteLine(args?.JsonStringify());
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (s, certificate, chain, sslPolicyErrors) => true;

            if (args!=null && args.Length > 0) 
            {
                if (args.Any(a => a == "-i"))
                {
                    InstallRootCA();
                    BuildRegistry();
                }
                else if (args[0].StartsWith("uxpki:",StringComparison.OrdinalIgnoreCase)) 
                {
                    //System.Diagnostics.Debugger.Launch();
                    //Uri uri = new Uri(args[0]);
                    //var queryString = QueryHelpers.ParseQuery(uri.Query);

                    //ReportSignerActivated(queryString);

                    ShowWindowAsync(Process.GetCurrentProcess().MainWindowHandle, SW_MINIMIZE);
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    var host = CreateHostBuilder(args)
                        .UseContentRoot(Path.GetDirectoryName(Environment.ProcessPath))
                        .Build();
                    host.Run();
                }
            }
            else 
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var host = CreateHostBuilder(args).Build();
                host.Run();
            }
            //Console.ReadKey();
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        private const int SW_HIDE = 0;
        private const int SW_NORMAL = 1;
        private const int SW_MAXIMIZE = 3;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWDEFAULT = 10;

        //private static void ReportSignerActivated(Dictionary<string, StringValues> queryString)
        //{
        //    Task.Run(() =>
        //    {
        //        Signable capi = new Signable();
        //        try
        //        {
        //            TWCASignDataViewModel viewModel = new TWCASignDataViewModel
        //            {
        //                TxnID = queryString["TxnID"],
        //                RemoteHost = queryString["RemoteHost"],
        //                SignatureAction = SignatureActionEnum.PushSignerActivation,
        //            };
        //            capi.PushSignature(viewModel);
        //            FileLogger.Logger.Info(viewModel.JsonStringify());

        //        }
        //        catch (Exception ex)
        //        {
        //            FileLogger.Logger.Error(ex);
        //        }
        //    });
        //}

        public static IHostBuilder CreateHostBuilder(string[]? args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.AddProvider(new FileLoggerProvider());
                    logging.AddDebug();
                    //logging.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public static void BuildRegistry()
        {
            if(File.Exists("RegPattern.txt"))
            {
                var regContent = File.ReadAllText("RegPattern.txt");
                Process proc = Process.GetCurrentProcess();
                var appName = proc.MainModule?.FileName?.Replace("\\", "\\\\");
                File.WriteAllText("uxpki.reg", regContent.Replace("{appName}", appName));

                ProcessStartInfo pi = new ProcessStartInfo();
                pi.UseShellExecute = true;
                pi.WorkingDirectory = Environment.CurrentDirectory;
                pi.FileName = "reg.exe";
                pi.Verb = "runas";
                pi.Arguments = $"import {Path.Combine(Directory.GetCurrentDirectory(),"uxpki.reg")}";
                Process.Start(pi);
                //Process.Start("reg", "import uxpki.reg");
            }
        }

        public static void InstallRootCA()
        {
            if (!String.IsNullOrEmpty(Settings.Default.RootCA))
            {
                try
                {
                    X509Certificate2 ca = new X509Certificate2(Settings.Default.RootCA);
                    X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);
                    var cert = store.Certificates.Cast<X509Certificate2>().Where(c => c.Thumbprint == ca.Thumbprint).FirstOrDefault();
                    if (cert == null)
                    {
                        store.Add(ca);
                    }
                    store.Close();

                    //store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
                    //store.Open(OpenFlags.ReadWrite);
                    //cert = store.Certificates.Cast<X509Certificate2>().Where(c => c.Thumbprint == ca.Thumbprint).FirstOrDefault();
                    //if (cert == null)
                    //{
                    //    store.Add(ca);
                    //}
                    //store.Close();

                }
                catch (Exception ex)
                {
                    FileLogger.Logger.Error(ex);
                }
            }
        }

        internal static void Terminate()
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
