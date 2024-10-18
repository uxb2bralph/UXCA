using CommonLib.Core.Utility;
using ContractHome.Models.DataEntity;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;

namespace ContractHome.Models.Rpt
{
    public class TaskProcess
    {
        public string FileName { get; set; }
        public string FileNo { get; set; }
        public string Email { get; set; }
        public string PublishedDateTime { get; set; }
        public string FinishedDateTime { get; set; }
        public IEnumerable<Operator> Operators { get; set; }
        public IEnumerable<Process> Processes { get; set; }
        public string TemplateItem => @$"~/Views/Report/{this.GetType().Name}.cshtml";


        public class Operator
        {
            public string Name { get; set; }
        }

        public class Process
        {
            public string Name { get; set; }
            public string DateTime { get; set; }
        }
    }
}
