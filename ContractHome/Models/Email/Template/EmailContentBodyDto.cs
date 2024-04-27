using ContractHome.Models.DataEntity;

namespace ContractHome.Models.Email.Template
{
    public class EmailContentBodyDto
    {
        public EmailContentBodyDto(Contract contract, Organization initiatorOrg, UserProfile userProfile)
        {
            Contract = contract;
            InitiatorOrg = initiatorOrg;
            UserProfile = userProfile;
        }

        public Contract? Contract {  get; }
        public Organization? InitiatorOrg { get; }
        public UserProfile? UserProfile { get; }
    }
}
