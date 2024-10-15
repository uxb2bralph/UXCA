using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Dto;
using Microsoft.EntityFrameworkCore;
using static ContractHome.Models.Dto.PostFieldSettingRequest;
using Wangkanai.Detection.Services;
using static ContractHome.Helper.JwtTokenGenerator;
using ContractHome.Helper.DataQuery;
using System.Text;
using static ContractHome.Models.DataEntity.CDS_Document;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.Xml;

namespace ContractHome.Models.Helper
{
    public class ContractServices
    {
        protected internal GenericManager<DCDataContext> _models;
        //protected internal Contract? _contract;
        private readonly EmailFactory _emailFactory;
        private readonly IDetectionService _detectionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly BaseResponse _baseResponse;
        private readonly EmailFactory _emailContentFactories;

        public ContractServices(IEmailBodyBuilder emailBody,
            EmailFactory emailFactory,
            IDetectionService detectionService,
            IHttpContextAccessor httpContextAccessor, 
            EmailFactory emailContentFactories,
            BaseResponse baseResponse
            ) 
        {
            _baseResponse = baseResponse;
            _emailFactory = emailFactory;
            _detectionService = detectionService;
            _httpContextAccessor = httpContextAccessor;
            _emailContentFactories = emailContentFactories;
        }

        private string _clientDevice => $"{_detectionService.Platform.Name} {_detectionService.Platform.Version.ToString()}/{_detectionService.Browser.Name}";
        private string _clientIP => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings?f1url=%3FappId%3Droslyn%26k%3Dk(CS8602)
        public static bool IsNotNull([NotNullWhen(true)] object? obj) => obj != null;

        public void SetModels(GenericManager<DCDataContext> models)
        {
            _models = models;
        }

        public enum DigitalSignCerts
        {
            Enterprise=0,//企業憑證
            UXB2B=1,//網優憑證
            Exchange=2,//以證換證
            //MOEA= 2,//工商憑證
            //MOI =3 //自然人憑證
        }

        public Contract GetContractByID(int? contractID)
        {
            return _models.GetTable<Contract>()
                    .Where(c => c.ContractID == contractID)
                    .FirstOrDefault();
        }

        //wait to do...replace by Contract.EntitySet<Organization>
        //public Organization? GetOrganization(Contract contract)
        //{
        //    return _models?.GetTable<Organization>()
        //        .Where(c => c.CompanyID == contract.CompanyID)
        //        .FirstOrDefault();
        //}

        public IEnumerable<Organization>? GetAvailableSignatories(int companyID)
        {
            return _models.GetTable<Organization>().Where(x => x.CompanyBelongTo == companyID);
        }

        public IEnumerable<UserProfile>? GetOperatorByCompanyID(int companyID)
        {
            return _models.GetTable<UserProfile>().Where(x => x.CompanyID == companyID);
        }

        public bool IsContractHasCompany(Contract contract, int? companyID)
        {
            return contract.ContractingParty.Where(x=>x.CompanyID == companyID).Any();
        }

        ////利用原有合約資料新增合約, for非聯合承攬用, 各別成立合約用
        ////同時新增CDS_Document及Contract資料
        //public Contract? CreateAndSaveContractByOld(Contract contract)
        //{
        //    var doc = _models.GetTable<CDS_Document>()
        //        .Where(d => d.DocID == contract.ContractID).First();

        //    if (doc==null)
        //    {
        //        throw new NullReferenceException();
        //    }

        //    try
        //    {
        //        String json = doc.JsonStringify();
        //        doc = JsonConvert.DeserializeObject<CDS_Document>(json);
        //        _models.GetTable<CDS_Document>().InsertOnSubmit(doc!);
        //        doc!.Contract.ContractSignaturePositionRequest = null;
        //        doc!.Contract.ContractingParty = null;
        //        doc!.Contract.ContractSignature = null;
        //        doc!.Contract.ContractSignatureRequest = null;
        //        //甲方起約時的用印也要複製一份到新的獨立合約
        //        //doc!.Contract.ContractSealRequest = null;
        //        doc!.Contract.ContractNoteRequest = null;
        //        _models.GetTable<Contract>().InsertOnSubmit(doc!.Contract);
        //        _models.SubmitChanges();
        //        return doc!.Contract;
        //    }
        //    catch (Exception ex)
        //    {
        //        FileLogger.Logger.Error(ex.ToString());
        //        throw;
        //    }
        //}

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
                    StampDate = (contract.IsPassStamp==true)?DateTime.Now:null,
                });
            }

            return contract;
        }

        public Contract DeleteAndCreateFieldPostion(Contract contract, 
            IEnumerable<PostFieldSettingRequestFields> feildSettings)
        {
            _models.DeleteAll<ContractSignaturePositionRequest>(x => x.ContractID == contract.ContractID);

            foreach (var pos in feildSettings)
            {
                if (!contract.ContractSignaturePositionRequest
                    .Where(p => p.ContractID == contract.ContractID)
                    .Where(p => p.OperatorID ==  pos.OperatorID.DecryptKeyValue())
                    .Where(p => p.ContractorID == pos.CompanyID.DecryptKeyValue())
                    .Where(p => p.PositionID == pos.ID)
                .Any())
                {
                    contract.ContractSignaturePositionRequest.Add(new ContractSignaturePositionRequest
                    {
                        ContractID = contract.ContractID,
                        ContractorID = null,
                        PositionID = pos.ID,
                        ScaleWidth = pos.ScaleWidth,
                        ScaleHeight = pos.ScaleHeight,
                        MarginTop = pos.MarginTop,
                        MarginLeft = pos.MarginLeft,
                        Type = (short)pos.Type,
                        PageIndex = pos.PageIndex,
                        OperatorID = (string.IsNullOrEmpty(pos.OperatorID)) ? null : pos.OperatorID.DecryptKeyValue()
                    });
                }

            }

            return contract;
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
                        Type = (short)pos.Type,
                        PageIndex = pos.PageIndex,
                        OperatorID = (string.IsNullOrEmpty(pos.OperatorID)) ? null : pos.OperatorID.DecryptKeyValue()
                    });
                }

            }

            return contract;
        }

        public (BaseResponse, Contract, UserProfile) CanPdfDigitalSign(int? contractID)
        {
            if (contractID == null || contractID == 0)
            {
                return (new BaseResponse(reason: WebReasonEnum.ContractNotExisted), null, null);
            }

            var profile = (_httpContextAccessor.HttpContext.GetUserAsync().Result).LoadInstance(_models);
            if (profile == null)
            {
                return (new BaseResponse(reason: WebReasonEnum.Relogin), null, null);
            }

            var item = GetContractByID(contractID: contractID);

            var parties = _models!.GetTable<ContractingParty>()
            .Where(p => p.ContractID == contractID)
            .Where(p => _models.GetTable<OrganizationUser>()
            .Where(o => o.UID == profile.UID).Any(o => o.CompanyID == p.CompanyID))
            .FirstOrDefault();

            if ((item == null) || (parties == null))
            {
                return (new BaseResponse(reason: WebReasonEnum.ContractNotExisted), item, profile);
            }

            if (item.CurrentStep >= (int)CDS_Document.StepEnum.DigitalSigned)
            {
                return (new BaseResponse(true, "合約已完成簽署流程, 無法再次簽署.").AddContractMessage(item), item, profile);
            }

            if (item.ContractSignatureRequest
                        .Where(x => x.CompanyID == profile.CompanyID)
                        .Where(x => x.SignatureDate != null).Count() > 0)
            {
                return (new BaseResponse(true, "合約已完成簽署, 無法再次簽署.").AddContractMessage(item), item, profile);
            }

            return (_baseResponse, item, profile);
        }

        public (BaseResponse, Contract) CanTaskSeal(int contractID, UserProfile userProfile)
        {
            if (contractID == null || contractID == 0)
            {
                return (new BaseResponse(reason: WebReasonEnum.ContractNotExisted), null);
            }

            if (userProfile == null)
            {
                return (new BaseResponse(reason: WebReasonEnum.Relogin), null);
            }

            var item = GetContractByID(contractID: contractID);
            if (item == null)
            {
                return (new BaseResponse(reason: WebReasonEnum.Relogin), item);
            }

            //wait to replace OrganizationUser by ContractingUser FOR Task
            if (!_models!.GetTable<ContractingUser>().Where(x => x.UserID == userProfile.UID).Any())
            {
                if (!_models!.GetTable<ContractingParty>()
                   .Where(p => p.ContractID == contractID)
                   .Where(p => _models.GetTable<OrganizationUser>()
                   .Where(o => o.UID == userProfile.UID).Any(o => o.CompanyID == p.CompanyID))
                   .Any())
                {
                    return (new BaseResponse(reason: WebReasonEnum.Relogin), item);
                }
            }

            if (item.CurrentStep >= (int)CDS_Document.StepEnum.Sealed)
            {
                return (new BaseResponse(true, "合約已完成用印流程, 無法再次用印.").AddContractMessage(item), item);
            }

            if (item.ContractUserSignatureRequest
                        .Where(x => x.UserID == userProfile.UID)
                        .Where(x => x.StampDate != null).Count() > 0)
            {
                return (new BaseResponse(true, "合約已完成用印, 無法再次用印.").AddContractMessage(item), item);
            }

            return (_baseResponse, item);
        }


        public (BaseResponse, Contract, UserProfile)  CanPdfSeal(int? contractID)
        {
            if (contractID==null||contractID == 0)
            {
                return (new BaseResponse(reason: WebReasonEnum.ContractNotExisted), null, null);
            }

            var profile = (_httpContextAccessor.HttpContext.GetUserAsync().Result).LoadInstance(_models);
            if (profile == null)
            {
                return (new BaseResponse(reason: WebReasonEnum.Relogin),null,null);
            }

            var item = GetContractByID(contractID:contractID);

            var parties = _models!.GetTable<ContractingParty>()
            .Where(p => p.ContractID == contractID)
            .Where(p => _models.GetTable<OrganizationUser>()
            .Where(o => o.UID == profile.UID).Any(o => o.CompanyID == p.CompanyID))
            .FirstOrDefault();

            if ((item == null) || (parties == null))
            {
                return (new BaseResponse(reason: WebReasonEnum.Relogin), item, profile);
            }

            if (item.CurrentStep >= (int)CDS_Document.StepEnum.Sealed)
            {
                return (new BaseResponse(true, "合約已完成用印流程, 無法再次用印.").AddContractMessage(item), item, profile);
            }

            if (item.ContractSignatureRequest
                        .Where(x => x.CompanyID == profile.CompanyID)
                        .Where(x => x.StampDate != null).Count() > 0)
            {
                return (new BaseResponse(true, "合約已完成用印, 無法再次用印.").AddContractMessage(item), item, profile);
            }


            //wait to do...同一份合約, 單邊只能一個人用印
            //if ((item.ContractSealRequest.Count() > 0) &&
            //    (!item.ContractSealRequest.Select(x => x.StampUID).Contains(profile.UID)))
            //{
            //    return (new BaseResponse(true, "合約已有其他人用印中"), item, profile);
            //}

            return (_baseResponse, item, profile);
        }

        //public Contract CreateAndSaveParty(int initiatorID, 
        //    int contractorID, 
        //    Contract contract,
        //    SignaturePosition[] SignaturePositions,
        //    int uid)
        //{
        //    if (contract == null) { return null; };

        //    #region 新增ContractingParty
        //    //移到[建立合約]下一洞新增ContractingParty,for甲方進行[聯合承攬]設定用印位置, 再進入[編輯合約]
        //    if (!contract.ContractingParty.Where(p => p.CompanyID == initiatorID)
        //        .Where(p => p.IntentID == (int)ContractingIntent.ContractingIntentEnum.Initiator)
        //        .Any())
        //    {
        //        contract.ContractingParty.Add(new ContractingParty
        //        {
        //            CompanyID = initiatorID,
        //            IntentID = (int)ContractingIntent.ContractingIntentEnum.Initiator,
        //            IsInitiator = true,
        //        });
        //    }

        //    if (!contract.ContractingParty.Where(p => p.CompanyID == contractorID)
        //                        .Where(p => p.IntentID == (int)ContractingIntent.ContractingIntentEnum.Contractor)
        //                        .Any())
        //    {
        //        contract.ContractingParty.Add(new ContractingParty
        //        {
        //            CompanyID = contractorID,
        //            IntentID = (int)ContractingIntent.ContractingIntentEnum.Contractor,
        //        });
        //    }
        //    #endregion

        //    #region 新增SignaturePositions
        //    //viewModel.SignaturePositions:
        //    //[聯合承攬]時, SignaturePositions綁原ContractID,
        //    //非[聯合承攬]時, SignaturePositions綁各別新增ContractID-->只能在新合約後做,才有新ContractID
        //    //-->要在同一頁指定乙方並挖洞做完, 因為現行找不回主約和其他複製合約的關係

        //    foreach (var pos in SignaturePositions)
        //    {

        //        if (!contract.ContractSignaturePositionRequest
        //            .Where(p => p.ContractID == contract.ContractID)
        //            .Where(p => p.ContractorID == contractorID)
        //            .Where(p => p.PositionID == pos.ID)
        //        .Any())
        //        {
        //            contract.ContractSignaturePositionRequest.Add(new ContractSignaturePositionRequest
        //            {
        //                ContractID = contract.ContractID,
        //                ContractorID = contractorID,
        //                PositionID = pos.ID,
        //                ScaleWidth = pos.ScaleWidth,
        //                ScaleHeight = pos.ScaleHeight,
        //                MarginTop = pos.MarginTop,
        //                MarginLeft = pos.MarginLeft,
        //                Type = (short)pos.Type,
        //                PageIndex = pos.PageIndex
        //            });
        //        }

        //    }
        //    #endregion

        //    #region 新增ContractSignatureRequest

        //    if (!contract.ContractSignatureRequest.Any(r => r.CompanyID == initiatorID))
        //    {
        //        contract.ContractSignatureRequest.Add(new ContractSignatureRequest
        //        {
        //            CompanyID = initiatorID,
        //            StampDate = DateTime.Now,
        //        });
        //    }

        //    if (!contract.ContractSignatureRequest.Any(r => r.CompanyID == contractorID))
        //    {
        //        contract.ContractSignatureRequest.Add(new ContractSignatureRequest
        //        {
        //            CompanyID = contractorID,
        //        });
        //    }
        //    #endregion

        //    _models.SubmitChanges();

        //    contract.CDS_Document.TransitStep(_models, uid, CDS_Document.StepEnum.Sealing);

        //    return contract;
        //}

        public IQueryable<UserProfile>? GetUsersbyCompanyID(int companyID)
        {
            if (companyID.Equals(0)) { return null; }
            var ttt = _models.GetTable<OrganizationUser>()
                .Where(x => x.CompanyID == companyID)
                .Select(y => y.UserProfile);
            return ttt;
        }

        public IQueryable<UserProfile>? GetUsersbyContract(Contract contract, bool isTask=false)
        {
            if (isTask)
            {
                return contract.ContractingUser.Select(x => x.UserProfile).AsQueryable();
            }

            return _models.GetTable<OrganizationUser>()
                .Where(x => contract.ContractingParty.Select(x=>x.CompanyID).Contains(x.CompanyID))
                .Select(y => y.UserProfile);
        }

        public IQueryable<UserProfile>? GetUsersByWhoNotFinished(Contract contract, 
            int currentStep)
        {
            if ((currentStep == 0) || (!CDS_Document.DocumentEditable.Contains((CDS_Document.StepEnum)currentStep!)))
                return null;

            bool isSigning = (contract.CurrentStep == (int)StepEnum.DigitalSigning || contract.CurrentStep == (int)StepEnum.Sealed);
            bool isStamping = (contract.CurrentStep == (int)StepEnum.Sealing);

            return _models.GetTable<OrganizationUser>()
                .Where(x => ((isSigning) ? contract.whoNotDigitalSigned():contract.whoNotStamped())
                            .Select(x => x.CompanyID).Contains(x.CompanyID))
                .Select(y => y.UserProfile);
        }

        public IEnumerable<UserProfile>? GetNotifyUsersAsync(Contract contract)
        {
            //EmailBody.EmailTemplate template = EmailBody.EmailTemplate.NotifySeal;

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

            return null;

        }


        public (BaseResponse, JwtToken, UserProfile) TokenValidate(string token)
        {
            try
            {
                token = token.GetEfficientString();

                if (string.IsNullOrEmpty(token))
                {
                    return (new BaseResponse(true, "驗證資料為空值。"), null, null);
                }

                if (!JwtTokenValidator.ValidateJwtToken(token, JwtTokenGenerator.secretKey))
                {
                    return (new BaseResponse(true, "Token已失效，請重新申請。"), null, null);
                }
                var jwtTokenObj = JwtTokenValidator.DecodeJwtToken(token);
                if (jwtTokenObj == null)
                {
                    return (new BaseResponse(true, "Token已失效，請重新申請。"), null, null);
                }

                UserProfile userProfile
                    = _models.GetTable<UserProfile>()
                        .Where(x => x.EMail.Equals(jwtTokenObj.Email))
                        .Where(x => x.UID.Equals(jwtTokenObj.UID.DecryptKeyValue()))
                        .FirstOrDefault();

                if (userProfile == null)
                {
                    return (new BaseResponse(true, "驗證資料有誤。"), jwtTokenObj, userProfile);
                }

                return (new BaseResponse(false, ""), jwtTokenObj, userProfile);
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error($"TokenValidate failed. JwtToken={token};   {ex}");
                return (new BaseResponse(true, "驗證資料有誤。"), null, null);
            }
        }

        public async void SendUsersNotifyEmailAboutContractAsync(
            Contract contract,
            IEmailContent emailContent,
            IQueryable<UserProfile> targetUsers)
        {
            if ((contract == null) || (targetUsers.Count() == 0) || (targetUsers == null)) return;

            if (targetUsers != null)
            {
                foreach (var user in targetUsers)
                {
                    EmailContentBodyDto emailContentBodyDto =
                        new EmailContentBodyDto(contract: contract, initiatorOrg: contract.Organization, userProfile: user);

                    emailContent.CreateBody(emailContentBodyDto);
                    _emailFactory.SendEmailToCustomer(mailTo: user.EMail, 
                        emailContent: emailContent);
                }
            }
        }

        public void SaveContract()
        {
            _models.SubmitChanges();
        }

        public void NotifyWhoNotFinishedDoc()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                _models = new GenericManager<DCDataContext>();

                var contracts = _models
                    .GetTable<Contract>()
                    .Where(d => !d.CDS_Document.CurrentStep.HasValue
                            || CDS_Document.DocumentEditable.Contains((CDS_Document.StepEnum)d.CDS_Document.CurrentStep!))
                    .Where(x => x.NotifyUntilDate != null)
                    .Where(x => x.NotifyUntilDate >= DateTime.Now.Date)
                    .ToList();

                if (contracts.Count()>0)
                {
                    sb.AppendLine($"NotifyWhoNotFinishedDoc:contracts.Count()={contracts.Count()}");

                    contracts.ForEach(contract =>
                    {
                        var users = GetUsersByWhoNotFinished(contract, contract.CurrentStep);
                        SendUsersNotifyEmailAboutContractAsync(
                                contract,
                                _emailContentFactories.GetNotifySign(),
                                users
                        );
                        var usersString = string.Join(" ", users.Select(x => $"{x.UID}"));
                        sb.Append($" ContractID: {contract.ContractID} {(CDS_Document.StepEnum)contract.CurrentStep} UID: {usersString}");
                    });
                } 
                else
                {
                    sb.AppendLine("NotifyWhoNotFinishedDoc:contracts.Count()=0");
                }
                FileLogger.Logger.Info(sb.ToString());

            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                throw;
            }

        }

        public Contract SetConfigAndSave(Contract contract, PostConfigRequest req, 
            int uid, bool isTask=false)
        {
            contract.ContractNo = req.ContractNo;
            contract.Title = req.Title;
            contract.IsPassStamp = req.IsPassStamp;
            req.Signatories.ForEach(x => {
                if (isTask)
                {
                    AddOperator(contract, x.DecryptKeyValue());
                }
                else
                {
                    AddParty(contract, x.DecryptKeyValue());
                }
            });
            contract.NotifyUntilDate = Convert.ToDateTime(req.ExpiryDateTime);
            SaveContract();

            CDS_DocumentTransitStep(contract, uid, CDS_Document.StepEnum.Config);
            return contract;
        }

        private Contract AddOperator(Contract contract, int OperatorID)
        {

            if (contract.ContractingUser.Where(p => p.UserID == OperatorID).Any())
            {
                return contract;
            }

            contract.ContractingUser.Add(new ContractingUser
            {
                UserID = OperatorID
            });

            if (!contract.ContractUserSignatureRequest.Any(r => r.UserID == OperatorID))
            {
                contract.ContractUserSignatureRequest.Add(new ContractUserSignatureRequest
                {
                    UserID = OperatorID,
                    StampDate = (contract.IsPassStamp == true) ? DateTime.Now : null,
                });
            }

            return contract;
        }

        public void CDS_DocumentTransitStep(
            Contract contract, 
            int uid,
            CDS_Document.StepEnum step)
        {
            contract.CDS_Document.DocumentProcessLog.Add(new DocumentProcessLog
            {
                LogDate = DateTime.Now,
                ActorID = uid,
                StepID = (int)step,
                ClientIP = _clientIP,
                ClientDevice = _clientDevice
            });

            SaveContract();

            if (step.Equals(CDS_Document.StepEnum.DigitalSigned) && !contract.isAllDigitalSignatureDone())
            {
                UpdateCDS_DocumentCurrentStep(contract, CDS_Document.StepEnum.DigitalSigning);
            }
            else 
            {
                UpdateCDS_DocumentCurrentStep(contract, step);
            }

        }

        public void UpdateCDS_DocumentCurrentStep(
            Contract contract,
            CDS_Document.StepEnum step)
        {
            contract.CDS_Document.CurrentStep = (int)step;
            SaveContract();
        }
    }

}
