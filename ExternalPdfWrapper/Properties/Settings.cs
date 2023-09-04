using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalPdfWrapper.Properties
{
    public class Settings : CommonLib.Utility.Properties.AppSettings
    {

        static Settings _default;

        public static Settings Default => _default;

        static Settings()
        {
            _default = Initialize<Settings>(typeof(Settings).Namespace);
        }

        public String Command { get; set; } = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe";
        public String ConvertPattern { get; set; } = "--headless --disable-gpu --run-all-compositor-stages-before-draw --print-to-pdf-no-header  --print-to-pdf={0} {1}";
        //"--headless --disable-gpu --run-all-compositor-stages-before-draw --print-to-pdf-no-header  --print-to-pdf={0} file://{1}";
        public String Args { get; set; }
    }
}
