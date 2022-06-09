using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Core.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TWCACAPIAdapter.Properties;

namespace TWCACAPIAdapter
{
    public class Program
    {
        public static void Main(string[]? args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InstallRootCA();
            CreateHostBuilder(args).Build().Run();
        }

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

    }
}
