using CommonLib.PlugInAdapter;
using ExternalPdfWrapper.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExternalPdfWrapper
{
    public class PdfUtility : IPdfUtility
    {

        public void ConvertHtmlToPDF(string htmlSource, string pdfFile, double timeOutInMinute, string[] args)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = Settings.Default.Command,
                Arguments = String.Concat(" ",
                                String.Format(Settings.Default.ConvertPattern, pdfFile, htmlSource.Contains("://") ? htmlSource : $"file://{htmlSource}"),
                                args != null && args.Length > 0 ? String.Join(" ", args) : ""),
                CreateNoWindow = true,
                //UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                //WorkingDirectory = AppDomain.CurrentDomain.RelativeSearchPath,
            };

            Process proc = new Process();
            proc.EnableRaisingEvents = true;
            //proc.Exited += new EventHandler(proc_Exited);

            //if (null != _eventHandler)
            //{
            //    proc.Exited += new EventHandler(_eventHandler);
            //}
            proc.StartInfo = info;
            proc.Start();
            proc.WaitForExit((int)timeOutInMinute * 60000);

        }

        public void ConvertHtmlToPDF(string htmlFile, string pdfFile, double timeOutInMinute)
        {
            ConvertHtmlToPDF(htmlFile, pdfFile, timeOutInMinute, null);
        }
    }
}
