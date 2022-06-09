using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CommonLib.Core.Properties
{
    public class Settings : CommonLib.Utility.Properties.AppSettings
    {
        static Settings _default;

        public static Settings Default => _default;

        static Settings()
        {
            _default = Initialize<Settings>(typeof(Settings).Namespace);
        }

        public String IPdfUtilityImpl { get; set; } = "WKPdfWrapper.PdfUtility,WKPdfWrapper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        public bool SqlLog { get; set; } = true;
        public bool EnableJobScheduler { get; set; } = true;
        public String LogPath { get; set; }
        public bool IgnoreCertificateRevoked { get; set; } = true;
    }

}
