namespace ContractHome.Models.Dto
{
    public class ClientUserInfo
    {
        public string UserName { get; set; }
        public string EUID { get; set; }
        public string CompanyName { get; set; }
        public bool IsMemberAdmin { get; set; }
        public bool IsSysAdmin { get; set; }
    }
}
