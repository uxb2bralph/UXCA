namespace ContractHome.Models.Report
{
    public class SignHistoryPager
    {
        public string FileName { get; set; } = string.Empty;
        public string FileNo { get; set; } = string.Empty;

        public string InitiatorName { get; set; } = string.Empty;

        public string InitiatorMail { get; set; } = string.Empty;

        public string CreateDateTime { get; set; } = string.Empty;

        public string FinishedDateTime { get; set; } = string.Empty;

        public IEnumerable<Signer> Signers { get; set; } = [];

        public IEnumerable<History> Histories { get; set; } = [];

        public string TemplateItem => @$"~/Views/Report/{this.GetType().Name}.cshtml";

        public class Signer
        {
            public string CompanyName { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public string Mail { get; set; } = string.Empty;

            public string Region { get; set; } = string.Empty;
        }

        public class History
        {
            public string CompanyName { get; set; } = string.Empty;

            public DateTime? LogDate { get; set; }

            public int StepID { get; set; } = 0;

            public string Mail { get; set; } = string.Empty;

            public string IP { get; set; } = string.Empty;

            public string Device { get; set; } = string.Empty;
        }
    }
}
