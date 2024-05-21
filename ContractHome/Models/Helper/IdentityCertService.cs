using ContractHome.Models.DataEntity;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Email;
using CommonLib.DataAccess;
using Org.BouncyCastle.Ocsp;

namespace ContractHome.Models.Helper
{
    public class IdentityCertService
    {
        protected IdentityCert identityCert;
        public IdentityCertService(IdentityCert identityCert
            //EmailBody emailBody,
            //    EmailFactory emailFactory
            )
        {
            //_emailBody = emailBody;
            //_emailFactory = emailFactory;
            //_contract = GetContractByID(contractID);
            this.identityCert = identityCert;
        }

    }

}
