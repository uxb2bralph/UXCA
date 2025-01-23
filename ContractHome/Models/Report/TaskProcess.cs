using CommonLib.Core.Utility;
using ContractHome.Models.DataEntity;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;

namespace ContractHome.Models.Report
{
    public class TaskProcess
    {
        public string FileName { get; set; } = string.Empty;
        public string FileNo { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public string TaskProcessDateTime { get; set; } = string.Empty;
        public string PublishedDateTime { get; set; } = string.Empty;
        public string FinishedDateTime { get; set; } = string.Empty;
        public IEnumerable<Operator>? Operators { get; set; }
        public IEnumerable<Process>? Processes { get; set; }
        public string TemplateItem => @$"~/Views/Report/{this.GetType().Name}.cshtml";


        public class Operator
        {
            public string Email { get; set; }
            private string Region { get; set; }
            public string RegionDesc =>
                this.Region.Equals("O") ? "工商憑證" : (this.Region.Equals("E") ? "企業憑證" : "其他");

            public Operator(string email, string region)
            {
                Email = email;
                Region = region;
            }
        }

        public class Process
        {
            public string TaskDateTime { get; set; }
            public string TaskDesc { get; set; }
            public string Email { get; set; }
            public string ClientIP { get; set; }
            public string ClientDevice { get; set; }

            public Process(string taskDateTime, string taskDesc, string email, string clientIP, string clientDevice)
            {
                TaskDateTime = taskDateTime;
                TaskDesc = taskDesc;
                Email = email;
                ClientIP = clientIP;
                ClientDevice = clientDevice;
            }
        }
    }
}
