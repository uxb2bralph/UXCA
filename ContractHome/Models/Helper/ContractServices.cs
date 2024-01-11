using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Email;
using ContractHome.Models.ViewModel;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;

namespace ContractHome.Models.Helper
{
    public class ContractServices
    {
        protected internal GenericManager<DCDataContext> _models;
        //protected internal Contract? _contract;
        private readonly EmailFactory _emailFactory;
        private readonly EmailBody _emailBody;
        public ContractServices(EmailBody emailBody,
            EmailFactory emailFactory) 
        {
            _emailBody = emailBody;
            _emailFactory = emailFactory;
            //_contract = GetContractByID(contractID);
        }

        public void SetModels(GenericManager<DCDataContext> models)
        {
            _models = models;
        }

        //public Contract? GetContract => _contract;

        public Contract? GetContractByID(int? contractID)
        {
            return _models.GetTable<Contract>()
                    .Where(c => c.ContractID == contractID)
                    .FirstOrDefault();
        }

        //利用原有合約資料新增合約, for非聯合承攬用, 各別成立合約用
        //同時新增CDS_Document及Contract資料
        public Contract? CreateAndSaveContractByOld(Contract contract)
        {
            var doc = _models.GetTable<CDS_Document>()
                .Where(d => d.DocID == contract.ContractID).First();

            if (doc==null)
            {
                throw new NullReferenceException();
            }

            try
            {
                String json = doc.JsonStringify();
                doc = JsonConvert.DeserializeObject<CDS_Document>(json);
                _models.GetTable<CDS_Document>().InsertOnSubmit(doc!);
                doc!.Contract.ContractSignaturePositionRequest = null;
                doc!.Contract.ContractingParty = null;
                doc!.Contract.ContractSignature = null;
                doc!.Contract.ContractSignatureRequest = null;
                //甲方起約時的用印也要複製一份到新的獨立合約
                //doc!.Contract.ContractSealRequest = null;
                doc!.Contract.ContractNoteRequest = null;
                _models.GetTable<Contract>().InsertOnSubmit(doc!.Contract);
                _models.SubmitChanges();
                return doc!.Contract;
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex.ToString());
                throw;
            }
        }

        public Contract CreateAndSaveParty(int initiatorID, 
            int contractorID, 
            Contract contract,
            SignaturePosition[] SignaturePositions,
            int uid)
        {
            if (contract == null) { return null; };

            #region 新增ContractingParty
            //移到[建立合約]下一洞新增ContractingParty,for甲方進行[聯合承攬]設定用印位置, 再進入[編輯合約]
            if (!contract.ContractingParty.Where(p => p.CompanyID == initiatorID)
                .Where(p => p.IntentID == (int)ContractingIntent.ContractingIntentEnum.Initiator)
                .Any())
            {
                contract.ContractingParty.Add(new ContractingParty
                {
                    CompanyID = initiatorID,
                    IntentID = (int)ContractingIntent.ContractingIntentEnum.Initiator,
                    IsInitiator = true,
                });
            }

            if (!contract.ContractingParty.Where(p => p.CompanyID == contractorID)
                                .Where(p => p.IntentID == (int)ContractingIntent.ContractingIntentEnum.Contractor)
                                .Any())
            {
                contract.ContractingParty.Add(new ContractingParty
                {
                    CompanyID = contractorID,
                    IntentID = (int)ContractingIntent.ContractingIntentEnum.Contractor,
                });
            }
            #endregion

            #region 新增SignaturePositions
            //viewModel.SignaturePositions:
            //[聯合承攬]時, SignaturePositions綁原ContractID,
            //非[聯合承攬]時, SignaturePositions綁各別新增ContractID-->只能在新合約後做,才有新ContractID
            //-->要在同一頁指定乙方並挖洞做完, 因為現行找不回主約和其他複製合約的關係

            foreach (var pos in SignaturePositions)
            {

                if (!contract.ContractSignaturePositionRequest
                    .Where(p => p.ContractID == contract.ContractID)
                    .Where(p => p.ContractorID == contractorID)
                    .Where(p => p.PositionID == pos.ID)
                .Any())
                {
                    contract.ContractSignaturePositionRequest.Add(new ContractSignaturePositionRequest
                    {
                        ContractID = contract.ContractID,
                        ContractorID = contractorID,
                        PositionID = pos.ID,
                        ScaleWidth = pos.ScaleWidth,
                        ScaleHeight = pos.ScaleHeight,
                        MarginTop = pos.MarginTop,
                        MarginLeft = pos.MarginLeft,
                        Type = pos.Type,
                        PageIndex = pos.PageIndex
                    });
                }

            }
            #endregion

            #region 新增ContractSignatureRequest

            if (!contract.ContractSignatureRequest.Any(r => r.CompanyID == initiatorID))
            {
                contract.ContractSignatureRequest.Add(new ContractSignatureRequest
                {
                    CompanyID = initiatorID,
                    StampDate = DateTime.Now,
                });
            }

            if (!contract.ContractSignatureRequest.Any(r => r.CompanyID == contractorID))
            {
                contract.ContractSignatureRequest.Add(new ContractSignatureRequest
                {
                    CompanyID = contractorID,
                });
            }
            #endregion

            _models.SubmitChanges();

            contract.CDS_Document.TransitStep(_models, uid, CDS_Document.StepEnum.InitiatorSealed);
            return contract;
        }


        public IQueryable<UserProfile>? GetUsersbyCompanyID(int companyID)
        {
            return _models.GetTable<OrganizationUser>()
                .Where(x => x.CompanyID == companyID)
                .Select(y => y.UserProfile);
        }

        public async IAsyncEnumerable<MailData> GetContractorNotifyEmailAsync(
            List<Contract> contracts,
            EmailBody.EmailTemplate emailTemplate)
        {
            foreach (var contract in contracts)
            {
                var initiatorOrg = contract.GetInitiator(_models)?.GetPartyOrganization(_models);
                var contractors = contract.GetContractor(_models);
                var contractorUsers = contractors.SelectMany(x => x.GetPartyUsers(_models));

                if ((initiatorOrg != null) && (initiatorOrg != null))
                {
                    foreach (var user in contractorUsers)
                    {
                        var emailBody =
                            new EmailBodyBuilder(_emailBody)
                            .SetTemplateItem(emailTemplate)
                            .SetContractNo(contract.ContractNo)
                            .SetTitle(contract.Title)
                            .SetUserName(initiatorOrg.CompanyName)
                            .SetRecipientUserName(user.UserName)
                            .SetRecipientUserEmail(user.EMail)
                            .Build();

                        yield return _emailFactory.GetEmailToCustomer(
                            emailBody.RecipientUserEmail,
                            _emailFactory.GetEmailTitle(emailTemplate),
                            await emailBody.GetViewRenderString());

                    }
                }
            }

            yield break;
        }

        public void SaveContract()
        {
            _models.SubmitChanges();
        }
    }

}
