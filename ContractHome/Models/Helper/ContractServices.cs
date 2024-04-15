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
using static ContractHome.Models.Dto.GetFieldSettingRequest;
using ContractHome.Models.Dto;
using MimeKit.Text;
using Microsoft.EntityFrameworkCore;
using static ContractHome.Models.Dto.PostFieldSettingRequest;
using Grpc.Core.Logging;
using static ContractHome.Helper.JwtTokenGenerator;
using System.Diagnostics;

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

        //wait to do...replace by Contract.EntitySet<Organization>
        public Organization? GetOrganization(Contract contract)
        {
            return _models?.GetTable<Organization>()
                .Where(c => c.CompanyID == contract.CompanyID)
                .FirstOrDefault();
        }

        public IEnumerable<Organization>? GetAvailableSignatories(int companyID)
        {
            return _models.GetTable<Organization>().Where(x => x.CompanyBelongTo == companyID);
        }

        public bool IsContractHasCompany(Contract contract, int? companyID)
        {
            return contract.ContractingParty.Where(x=>x.CompanyID == companyID).Any();
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

        public Contract SetConfig(Contract contract, PostConfigRequest req)
        {
            contract.ContractNo = req.ContractNo;
            contract.Title = req.Title;
            req.Signatories.ForEach(x => { 
                AddParty(contract, x.DecryptKeyValue()); 
            });

            return contract;
        }

        public Contract AddParty(Contract contract, int CompanyID)
        {

            if (contract.ContractingParty.Where(p => p.CompanyID == CompanyID).Any()) 
            {
                return contract;
            }

            contract.ContractingParty.Add(new ContractingParty
            {
                CompanyID = CompanyID,
                IntentID = (contract.CompanyID.Equals(CompanyID))?1:2,
                IsInitiator = (contract.CompanyID.Equals(CompanyID)),
            });

            if (!contract.ContractSignatureRequest.Any(r => r.CompanyID == CompanyID))
            {
                contract.ContractSignatureRequest.Add(new ContractSignatureRequest
                {
                    CompanyID = CompanyID,
                    StampDate = null,
                });
            }

            return contract;
        }

        public class FeildSetting
        {
            public int CompanyID { get; set; }
            public string ID { get; set; }
            public double ScaleWidth { get; set; }
            public double ScaleHeight { get; set; }
            public double MarginTop { get; set; }
            public double MarginLeft { get; set; }
            public int PageIndex { get; set; }
            //0:default 1:文字 2.地址 3.電話 4.日期 5.公司Title 6.印章 7.簽名 8.圖片 ... 擴充?
            public int Type { get; set; }
        }

        public Contract UpdateFieldSetting(Contract contract, IEnumerable<PostFieldSettingRequestFields> feildSettings)
        {
            _models.DeleteAll<ContractSignaturePositionRequest>(x => x.ContractID == contract.ContractID);

            foreach (var pos in feildSettings)
            {
                int ttt = pos.CompanyID.DecryptKeyValue();
                if (!contract.ContractSignaturePositionRequest
                    .Where(p => p.ContractID == contract.ContractID)
                    .Where(p => p.ContractorID == pos.CompanyID.DecryptKeyValue())
                    .Where(p => p.PositionID == pos.ID)
                .Any())
                {
                    contract.ContractSignaturePositionRequest.Add(new ContractSignaturePositionRequest
                    {
                        ContractID = contract.ContractID,
                        ContractorID = pos.CompanyID.DecryptKeyValue(),
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

            return contract;
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

            contract.CDS_Document.TransitStep(_models, uid, CDS_Document.StepEnum.Sealing);

            return contract;
        }


        public IQueryable<UserProfile>? GetUsersbyCompanyID(int companyID)
        {
            if (companyID.Equals(0)) { return null; }
            var ttt = _models.GetTable<OrganizationUser>()
                .Where(x => x.CompanyID == companyID)
                .Select(y => y.UserProfile);
            return ttt;
        }

        public async IAsyncEnumerable<MailData> GetAllContractUsersNotifyEmailAsync(
            List<Contract> contracts,
            EmailBody.EmailTemplate emailTemplate)
        {
            foreach (var contract in contracts)
            {
                var initiatorOrg = contract.GetInitiator()?.GetOrganization(_models);
                var users = contract.ContractingParty.SelectMany(x => x.GetUsers(_models));

                if ((initiatorOrg != null) && (initiatorOrg != null))
                {
                    foreach (var user in users)
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

        //public enum NotifyFlagEnum
        //{
        //    None = 0,  // 無通知
        //    Signature = 1,   // 用印簽署通知
        //    Finish = 2,   // 完成通知
        //}

        //public NotifyFlagEnum GetNotifyFlagAsync(Contract contract)
        //{

        //    //items = items.Where(d => !d.CDS_Document.CurrentStep.HasValue
        //    //    || CDS_Document.PendingState.Contains((CDS_Document.StepEnum)d.CDS_Document.CurrentStep!));
        //    //CDS_Document.StepEnum

        //}

        public IEnumerable<UserProfile>? GetNotifyUsersAsync(Contract contract)
        {
            EmailBody.EmailTemplate template = EmailBody.EmailTemplate.NotifySeal;

            if ((contract.CDS_Document.CurrentStep.Equals((int)CDS_Document.StepEnum.Establish)||
                (contract.CDS_Document.CurrentStep.Equals((int)CDS_Document.StepEnum.DigitalSigning))))
            {
                //wait to do...新增簽署人時新增簽署順序,並記錄在ContractSignatureRequest
                var ttt = contract.ContractSignatureRequest
                    .Where(x=>x.StampDate==null);
                var aaa = ttt.Where(x => x.CompanyID != contract.CompanyID).FirstOrDefault();
                var bbb = ttt.Where(x => x.CompanyID == contract.CompanyID).FirstOrDefault();
                if (aaa!=null)
                {
                    return GetUsersbyCompanyID(aaa.CompanyID);
                }
                else if (bbb!=null)
                {
                    return GetUsersbyCompanyID(bbb.CompanyID);
                }
            }

            //if (contract.CDS_Document.CurrentStep.Equals((int)CDS_Document.StepEnum.DigitalSigned))
            //{

            //}

            //if (contract.CDS_Document.CurrentStep.Equals((int)CDS_Document.StepEnum.Committed))
            //{

            //}

            return null;

        }

        public async IAsyncEnumerable<MailData> GetNotifyEmailBodyAsync(
            Contract contract,
            IEnumerable<UserProfile> userProfiles,
            EmailBody.EmailTemplate emailTemplate,
            string defaultUri)
        {
            var initiatorOrg = GetOrganization(contract);

            if (initiatorOrg != null)
            {
                foreach (var user in userProfiles)
                {

                    JwtPayloadData jwtPayloadData = new JwtPayloadData()
                    {
                        UID = user.UID,
                        Email = user.EMail,
                        ContractID = contract.ContractID.ToString()
                    };
                    var jwtToken = JwtTokenGenerator.GenerateJwtToken(jwtPayloadData, 4320);
                    var clickLink = $"{defaultUri}/Account/SignatureTrust?token={jwtToken}";

                    var emailBody =
                        new EmailBodyBuilder(_emailBody)
                        .SetTemplateItem(emailTemplate)
                        .SetContractNo(contract.ContractNo)
                        .SetTitle(contract.Title)
                        .SetUserName(initiatorOrg.CompanyName)
                        .SetRecipientUserName(user.UserName)
                        .SetRecipientUserEmail(user.EMail)
                        .SetContractLink(clickLink)
                        .Build();

                    yield return _emailFactory.GetEmailToCustomer(
                        emailBody.RecipientUserEmail,
                        _emailFactory.GetEmailTitle(emailTemplate),
                        await emailBody.GetViewRenderString());

                }
                yield break;
            }


        }

        public async IAsyncEnumerable<MailData> GetContractorNotifyEmailAsync(
            List<Contract> contracts,
            EmailBody.EmailTemplate emailTemplate)
        {

            foreach (var contract in contracts)
            {
                var initiatorOrg = contract.GetInitiator()?.GetOrganization(_models);
                var contractors = contract.GetContractor();
                var contractorUsers = contractors.SelectMany(x => x.GetUsers(_models));

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
