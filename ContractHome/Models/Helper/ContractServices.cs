using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Email.Template;
using ContractHome.Models.Email;
using ContractHome.Models.ViewModel;
using Newtonsoft.Json;
using ContractHome.Models.Dto;
using Microsoft.EntityFrameworkCore;
using static ContractHome.Models.Dto.PostFieldSettingRequest;
using Wangkanai.Detection.Services;
using ContractHome.Models.Cache;
using Microsoft.AspNetCore.Authorization;
using static ContractHome.Helper.JwtTokenGenerator;
using ContractHome.Helper.DataQuery;
using ContractHome.Properties;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using static ContractHome.Models.DataEntity.CDS_Document;
using DocumentFormat.OpenXml.Drawing;
using ContractHome.Controllers;
using System.Diagnostics.CodeAnalysis;

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
                        PageIndex = pos.PageIndex
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
                        Type = (short)pos.Type,
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

        public IQueryable<UserProfile>? GetUsersbyContract(Contract contract)
        {
            DCDataContext db = _models.DataContext;

            // 抓 ContractSignatureRequest SignerID 不為 NULL 的 UserProfile
            var signers = from u in db.UserProfile
                          join c in db.ContractSignatureRequest on u.UID equals c.SignerID
                          where c.ContractID == contract.ContractID
                          select u;
            // 抓 ContractSignatureRequest SignerID 為 NULL 的 OrganizationUser
            var orgUsers = from c in db.ContractSignatureRequest
                           join ou in db.OrganizationUser on c.CompanyID equals ou.CompanyID
                           join u in db.UserProfile on ou.UID equals u.UID
                           where c.ContractID == contract.ContractID && c.SignerID == null
                           select u;
            // 合併兩個查詢結果
            var users = signers.Union(orgUsers);

            return users;
        }

        /// <summary>
        /// 取得未簽署合約的使用者
        /// </summary>
        /// <param name="contract"></param>
        /// <returns></returns>
        public IQueryable<UserProfile> GetNoSignUsers(Contract contract)
        {
            DCDataContext db = _models.DataContext;
            // 抓 ContractSignatureRequest SignerID 不為 NULL 且 未簽署 的 UserProfile
            var noSignUsers = from u in db.UserProfile
                              join c in db.ContractSignatureRequest on u.UID equals c.SignerID
                              where c.ContractID == contract.ContractID && 
                                    c.SignatureDate == null && 
                                    c.StampDate != null
                              select u;
            // 抓 ContractSignatureRequest SignerID 為 NULL 且 未簽署 的 UserProfile
            var noSignOrgUsers = from c in db.ContractSignatureRequest
                                 join ou in db.OrganizationUser on c.CompanyID equals ou.CompanyID
                                 join u in db.UserProfile on ou.UID equals u.UID
                                 where c.ContractID == contract.ContractID && 
                                       c.SignerID == null && 
                                       c.SignatureDate == null && 
                                       c.StampDate != null
                                 select u;

            // 合併兩個查詢結果
            var users = noSignUsers.Union(noSignOrgUsers);

            return users;
        }
        /// <summary>
        /// 取得未用印的使用者
        /// </summary>
        /// <param name="contract"></param>
        /// <returns></returns>
        public IQueryable<UserProfile> GetNoSealUsers(Contract contract)
        {
            DCDataContext db = _models.DataContext;
            // 抓 ContractSignatureRequest SignerID 不為 NULL 且 未用印 的 UserProfile
            var noSealUsers = from u in db.UserProfile
                              join c in db.ContractSignatureRequest on u.UID equals c.SignerID
                              where c.ContractID == contract.ContractID && 
                                    c.StampDate == null
                              select u;
            // 抓 ContractSignatureRequest SignerID 為 NULL 且 未用印 的 UserProfile
            var noSealOrgUsers = from c in db.ContractSignatureRequest
                                 join ou in db.OrganizationUser on c.CompanyID equals ou.CompanyID
                                 join u in db.UserProfile on ou.UID equals u.UID
                                 where c.ContractID == contract.ContractID && 
                                       c.SignerID == null && 
                                       c.StampDate == null
                                 select u;
            // 合併兩個查詢結果
            var users = noSealUsers.Union(noSealOrgUsers);
            return users;
        }

        /// <summary>
        /// 取得合約簽署人
        /// </summary>
        /// <param name="contract"></param>
        /// <returns></returns>
        public IQueryable<UserProfile>? GetUsersByContractSignatureRequest(Contract contract)
        {
            DCDataContext db = _models.DataContext;
            return from u in db.UserProfile
                        join c in db.ContractSignatureRequest on u.UID equals c.SignerID
                        where c.ContractID == contract.ContractID
                        select u;
        }

        public IQueryable<UserProfile>? GetUsersByWhoNotFinished(Contract contract, 
            int currentStep)
        {
            if ((currentStep == 0) || (!CDS_Document.DocumentEditable.Contains((CDS_Document.StepEnum)currentStep!)))
                return null;

            DCDataContext db = _models.DataContext;

            // 抓 ContractSignatureRequest SignerID 不為 NULL 且 未用印 未簽署 的 UserProfile 
            var signers = from u in db.UserProfile
                          join c in db.ContractSignatureRequest on u.UID equals c.SignerID
                          where c.ContractID == contract.ContractID && (c.SignatureDate == null || c.StampDate == null)
                          select u;
            // 抓 ContractSignatureRequest SignerID 為 NULL 的 OrganizationUser
            var orgUsers = from c in db.ContractSignatureRequest
                           join ou in db.OrganizationUser on c.CompanyID equals ou.CompanyID
                           join u in db.UserProfile on ou.UID equals u.UID
                           where c.ContractID == contract.ContractID && c.SignerID == null
                           select u;
            // 合併兩個查詢結果
            var users = signers.Union(orgUsers);

            return users;
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

        /// <summary>
        /// 是否是合約發起公司
        /// </summary>
        /// <param name="contract"></param>
        /// <param name="companyID"></param>
        /// <returns></returns>
        public bool IsContractInitiatorCompany(Contract contract, UserProfile profile)
        {
            DCDataContext db = _models.DataContext;

            return (from c in db.ContractingParty
                    join o in db.Organization on c.CompanyID equals o.CompanyID
                    join u in db.OrganizationUser on o.CompanyID equals u.CompanyID
                    where c.ContractID == contract.ContractID && 
                          c.IsInitiator == true && 
                          u.UID == profile.UID
                    select c.CompanyID)
                    .Any();
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

        public async Task SendUsersNotifyEmailAboutContractAsync(
            Contract contract,
            IEmailContent emailContent,
            IQueryable<UserProfile> targetUsers)
        {
            if ((contract == null) || (targetUsers.Count() == 0) || (targetUsers == null)) return;

            var initiatorOrg = GetOrganization(contract);
            //var userProfiles = GetUsersbyContract(contract);

            if (targetUsers != null)
            {
                foreach (var user in targetUsers)
                {
                    EmailContentBodyDto emailContentBodyDto =
                        new EmailContentBodyDto(contract: contract, initiatorOrg: initiatorOrg, userProfile: user);

                    emailContent.CreateBody(emailContentBodyDto);
                    await _emailFactory.SendEmailToCustomer(user.EMail,
                        emailContent);
                }
            }
        }

        public void SaveContract()
        {
            _models.SubmitChanges();
        }

        /// <summary>
        /// 合約終止
        /// </summary>
        /// <param name="contract"></param>
        public void TerminationContractByInitiator(Contract contract)
        {
            var db = _models.DataContext;
            // 取得合約建立人
            var initiator = (from dp in db.DocumentProcessLog
                                join u in db.UserProfile on dp.ActorID equals u.UID
                                where dp.DocID == contract.ContractID && dp.StepID == (int)CDS_Document.StepEnum.Establish
                                select u).FirstOrDefault();

            if (initiator == null)
            {
                FileLogger.Logger.Info($"TerminationContractByInitiator Contract: {contract.Title}-{contract.ContractNo} 合約建立人不存在!?");
                return;
            }
            // 合約終止
            DocumentProcessLog terminatedLog = new()
            {
                LogDate = DateTime.Now,
                ActorID = initiator.UID,
                StepID = (int)CDS_Document.StepEnum.Terminated,
                ClientIP = "-1",
                ClientDevice = "System"
            };

            contract.CDS_Document.DocumentProcessLog.Add(terminatedLog);
            contract.CDS_Document.CurrentStep = (int)CDS_Document.StepEnum.Terminated;
            _models.SubmitChanges();
            FileLogger.Logger.Info($"TerminationContractByInitiator Contract: {contract.Title}-{contract.ContractNo} 已終止");
        }

        public async Task TerminationContractFlow()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                _models = new GenericManager<DCDataContext>();
                var contracts = _models
                    .GetTable<Contract>()
                    .Where(d => CDS_Document.DocumentEditable.Contains((CDS_Document.StepEnum)d.CDS_Document.CurrentStep!))
                    .Where(x => x.NotifyUntilDate != null)
                    .Where(x => x.NotifyUntilDate.Value.AddDays(1) == DateTime.Now.Date)
                    .ToList();
                if (contracts.Count > 0)
                {
                    sb.AppendLine($"TerminationContractFlow:contracts.Count()={contracts.Count()}");
                    foreach (var contract in contracts)
                    {
                        await NotifyTerminationContract(contract);
                        TerminationContractByInitiator(contract);
                    }
                }
                else
                {
                    sb.AppendLine("TerminationContractFlow:contracts.Count()=0");
                }
                FileLogger.Logger.Info(sb.ToString());
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// 發送合約終止通知
        /// </summary>
        /// <returns></returns>
        public async Task NotifyTerminationContract(Contract contract)
        {
            var users = GetUsersByWhoNotFinished(contract, contract.CurrentStep);
            await SendUsersNotifyEmailAboutContractAsync(
                        contract,
                        _emailContentFactories.GetTerminationContract(),
                        users
            );

            var usersString = string.Join(" ", users.Select(x => $"{x.UID}"));
            FileLogger.Logger.Info($"NotifyTerminationContract ContractID: {contract.ContractID} {(CDS_Document.StepEnum)contract.CurrentStep} UID: {usersString}");
        }

        public async Task NotifyWhoNotFinishedDoc()
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
                    .Where(x => x.NotifyUntilDate.Value.AddDays(-1) == DateTime.Now.Date || x.NotifyUntilDate == DateTime.Now.Date)
                    .ToList();

                if (contracts.Count()>0)
                {
                    sb.AppendLine($"NotifyWhoNotFinishedDoc:contracts.Count()={contracts.Count}");

                    foreach (var contract in contracts)
                    {
                        var users = GetUsersByWhoNotFinished(contract, contract.CurrentStep);
                        await SendUsersNotifyEmailAboutContractAsync(
                                    contract,
                                    _emailContentFactories.GetNotifySign(),
                                    users
                        );
                        var usersString = string.Join(" ", users.Select(x => $"{x.UID}"));
                        sb.Append($" ContractID: {contract.ContractID} {(CDS_Document.StepEnum)contract.CurrentStep} UID: {usersString}");
                    }
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

        public Contract SetConfigAndSave(Contract contract, PostConfigRequest req, int uid)
        {
            contract.ContractNo = req.ContractNo;
            contract.Title = req.Title;
            contract.IsPassStamp = req.IsPassStamp;
            req.Signatories.ForEach(x => {
                AddParty(contract, x.DecryptKeyValue());
            });

            if (!string.IsNullOrWhiteSpace(req.ExpiryDateTime))
            {
                contract.NotifyUntilDate = Convert.ToDateTime(req.ExpiryDateTime);
            }
            SaveContract();

            CDS_DocumentTransitStep(contract, uid, CDS_Document.StepEnum.Config);

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
