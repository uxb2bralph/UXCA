using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CommonLib.Core.Properties
{
    public partial class Settings : AppSettings
    {
        static Settings()
        {
            _default = Initialize<Settings>(typeof(Settings).Namespace);
        }

        public Settings() : base()
        {

        }

        static Settings _default;
        public new static Settings Default
        {
            get
            {
                return _default;
            }
        }

        public String IPdfUtilityImpl { get; set; } = "WKPdfWrapper.PdfUtility,WKPdfWrapper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        public bool SqlLog { get; set; } = true;
        public bool EnableJobScheduler { get; set; } = true;
        public String LogPath { get; set; }
        public bool IgnoreCertificateRevoked { get; set; } = true;
    }
}
