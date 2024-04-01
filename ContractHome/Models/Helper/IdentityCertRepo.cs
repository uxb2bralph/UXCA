using ContractHome.Models.DataEntity;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Email;
using CommonLib.DataAccess;
using Org.BouncyCastle.Ocsp;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ContractHome.Models.Helper
{
    public class IdentityCertRepo
    {
        protected internal GenericManager<DCDataContext> _models;
        public IdentityCertRepo(
            //EmailBody emailBody,
            //    EmailFactory emailFactory
            GenericManager<DCDataContext> models
            )
        {
            //_emailBody = emailBody;
            //_emailFactory = emailFactory;
            //_contract = GetContractByID(contractID);
            _models = models;
        }

        public IEnumerable<IdentityCert> Get(string x509String, int uid)
        {
            return _models.GetTable<IdentityCert>()
                .Where(x => x.X509Certificate.Equals(x509String))
                .Where(x => x.BindingUID == uid);
        }

        public IEnumerable<IdentityCert> GetByUid(int uid)
        {
            return _models.GetTable<IdentityCert>()
                .Where(x => x.BindingUID == uid);
        }

        public IdentityCert GetBySeqNo(int seqNo)
        {
            return _models.GetTable<IdentityCert>()
                .Where(x => x.SeqNo==seqNo).FirstOrDefault();
        }

        public void DeleteSubmitChanges(int seqNo)
        {
            var identityCert = GetBySeqNo(seqNo: seqNo);
            _models.GetTable<IdentityCert>().DeleteOnSubmit(identityCert);
            _models.SubmitChanges();
        }

        public void AddSubmitChanges(IdentityCert identityCert)
        {
            _models.GetTable<IdentityCert>().InsertOnSubmit(identityCert);
            _models.SubmitChanges();
        }
    }

}
