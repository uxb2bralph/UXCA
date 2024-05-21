namespace ContractHome.Models.Dto
{
    public class DigitalSignModal
    {
        public string ContractNo { get; set; }
        public string ContractTitle { get; set; }
        public string CompanyName { get; set; }
        public string ContractID { get; set; }

        public override string? ToString()
        {
            return $"ContractNo={ContractNo} ContractTitle={ContractTitle} CompanyName={CompanyName} ContractID={ContractID}";
        }
    }
}
