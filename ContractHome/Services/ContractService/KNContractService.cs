using System.ComponentModel.DataAnnotations;
using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Services.HttpChunk;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using CommonLib.Utility;
using ContractHome.Models.Email.Template;
using Hangfire;
using Newtonsoft.Json.Linq;
using ContractHome.Models.Rpt;
using ContractHome.Models.Report;
using static ContractHome.Models.Report.SignHistoryPager;
using System.Threading.Tasks;

namespace ContractHome.Services.ContractService
{
    /// <summary>
    /// 中鋼KN合約服務
    /// </summary>
    public class KNContractService(IHttpChunkService httpChunkService, IOptions<KNFileUploadSetting> kNFileUploadSetting, EmailFactory emailFactory, IViewRenderService viewRenderService, ChunkFileUploader chunkFileUploader) : ICustomContractService
    {

        private readonly IHttpChunkService _httpChunkService = httpChunkService;
        private readonly KNFileUploadSetting _KNFileUploadSetting = kNFileUploadSetting.Value;
        private readonly EmailFactory _emailFactory = emailFactory;
        private readonly IViewRenderService _viewRenderService = viewRenderService;
        private readonly ChunkFileUploader _chunkFileUploader = chunkFileUploader;

        /// <summary>
        /// 起約人資訊
        /// </summary>
        public class Promisor
        {
            public int UID { get; set; }
            public string EMail { get; set; } = string.Empty;
            public int CompanyID { get; set; }
        }

        /// <summary>
        /// 建立合約
        /// </summary>
        /// <param name="db"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private (Contract contract, Promisor promisor) CreateContract(DCDataContext db, ContractModel model)
        {
            // 取得起約人資訊
            var promisor = (from u in db.UserProfile
                            join ou in db.OrganizationUser on u.UID equals ou.UID
                            where u.EMail.Equals(model.NotifyMail)
                            select new Promisor()
                            {
                                UID = u.UID,
                                EMail = u.EMail,
                                CompanyID = ou.CompanyID
                            }).FirstOrDefault();

            // 建立合約
            Contract contract = new()
            {
                FilePath = GetContractFile(model.ContractNo)?.FullName,
                ContractNo = model.ContractNo,
                Title = model.Title,
                IsPassStamp = model.IsPassStamp,
                CompanyID = promisor?.CompanyID ?? 0,
                NotifyUntilDate = DateTime.Parse(model.ExpiryDateTime)
            };

            // 合約步驟資訊
            CDS_Document cds = new()
            {
                DocDate = DateTime.Now,
                ProcessType = (int)CDS_Document.ProcessTypeEnum.PDF,
                CurrentStep = (model.IsPassStamp) ? (int)CDS_Document.StepEnum.Establish : (int)CDS_Document.StepEnum.Config,
            };

            contract.CDS_Document = cds;

            db.Contract.InsertOnSubmit(contract);
            db.SubmitChanges();

            return (contract, promisor);
        }

        /// <summary>
        /// 建立起約者合約簽署要求
        /// </summary>
        /// <param name="contract"></param>
        /// <param name="companyId"></param>
        /// <param name="uid"></param>
        /// <param name="isPassStamp"></param>
        private void CreateContractSignatureRequest(Contract contract, int companyId, int uid, bool isPassStamp)
        {
            // 建立起約者合約簽署要求
            ContractSignatureRequest promisorCSR = new()
            {
                CompanyID = companyId,
                SignerID = uid,
            };
            if (isPassStamp)
            {
                promisorCSR.StampDate = DateTime.Now;
            }
            contract.ContractSignatureRequest.Add(promisorCSR);
        }

        /// <summary>
        /// 建立相關合約公司
        /// </summary>
        /// <param name="contract"></param>
        /// <param name="companyId"></param>
        /// <param name="intentID"></param>
        /// <param name="isInitiator"></param>
        private void CreateContractingParty(Contract contract, int companyId, int intentID, bool isInitiator)
        {
            ContractingParty cp = new()
            {
                CompanyID = companyId,
                IntentID = intentID,
                IsInitiator = isInitiator
            };

            contract.ContractingParty.Add(cp);
        }

        /// <summary>
        /// 建立簽署者公司
        /// </summary>
        /// <param name="db"></param>
        /// <param name="signatory"></param>
        /// <param name="companyBelongTo"></param>
        /// <returns></returns>
        private int CreateOrganization(DCDataContext db, Signatory signatory, int companyBelongTo)
        {
            // 建議簽屬者公司是否存在
            var signatoryOrg = (from o in db.Organization
                                where o.ReceiptNo.Equals(signatory.Identifier)
                                select new
                                {
                                    o.CompanyID
                                }).FirstOrDefault();
            // 建議簽屬者公司
            int companyId = signatoryOrg?.CompanyID ?? 0;
            if (companyId == 0)
            {
                Organization organization = new()
                {
                    CompanyName = signatory.Name,
                    ReceiptNo = signatory.Identifier,
                    CompanyBelongTo = companyBelongTo,
                    CanCreateContract = false
                };

                db.Organization.InsertOnSubmit(organization);
                db.SubmitChanges();
                companyId = organization.CompanyID;
            }

            return companyId;
        }

        /// <summary>
        /// 建立簽署者
        /// </summary>
        /// <param name="db"></param>
        /// <param name="signatory"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        private int CreateUserProfile(DCDataContext db, Signatory signatory, int companyId)
        {
            // 檢查簽屬者是否存在
            var signatoryUser = (from u in db.UserProfile
                                 join ou in db.OrganizationUser on u.UID equals ou.UID
                                 join o in db.Organization on ou.CompanyID equals o.CompanyID
                                 where u.EMail.Equals(signatory.Mail) && o.ReceiptNo.Equals(signatory.Identifier)
                                 select new
                                 {
                                     u.UID,
                                     u.EMail,
                                     ou.CompanyID
                                 }).FirstOrDefault();

            // 建立簽屬者
            int uid = signatoryUser?.UID ?? 0;
            if (uid == 0)
            {
                UserProfile userProfile = new()
                {
                    EMail = signatory.Mail,
                    PID = signatory.Mail,
                    Password = $"@{signatory.Mail}".HashPassword(),
                    Region = "0"
                };

                db.UserProfile.InsertOnSubmit(userProfile);
                db.SubmitChanges();
                uid = userProfile.UID;

                OrganizationUser organizationUser = new()
                {
                    CompanyID = companyId,
                    UID = userProfile.UID,
                };

                db.OrganizationUser.InsertOnSubmit(organizationUser);
                db.SubmitChanges();
            }

            return uid;
        }

        /// <summary>
        /// 建立合約步驟資訊
        /// </summary>
        /// <param name="contract"></param>
        /// <param name="uid"></param>
        /// <param name="isPassStamp"></param>
        private void CreateDocumentProcessLog(Contract contract, int uid, bool isPassStamp)
        {
            DocumentProcessLog establishLog = new()
            {
                LogDate = DateTime.Now,
                ActorID = uid,
                StepID = (isPassStamp) ? (int)CDS_Document.StepEnum.Establish : (int)CDS_Document.StepEnum.Config,
                ClientIP = "-1",
                ClientDevice = "System"
            };

            contract.CDS_Document.DocumentProcessLog.Add(establishLog);
        }

        /// <summary>
        /// 發送Mail通知
        /// </summary>
        /// <param name="contract"></param>
        /// <param name="sendMails"></param>
        /// <param name="emailContent"></param>
        private async void SendMail(Contract contract, List<string> sendMails, IEmailContent emailContent)
        {
            using DCDataContext db = new();

            var targetUsers = (from u in db.UserProfile
                               where sendMails.Contains(u.EMail)
                               select u).AsEnumerable<UserProfile>();

            var initiatorOrg = (from o in db.Organization
                                where o.CompanyID == contract.CompanyID
                                select o).FirstOrDefault();

            if (initiatorOrg == null)
            {
                return;
            }

            foreach (var user in targetUsers)
            {
                EmailContentBodyDto emailContentBodyDto = new(contract, initiatorOrg, user);

                emailContent.CreateBody(emailContentBodyDto);
                await _emailFactory.SendEmailToCustomer(user.EMail, emailContent);
            }

            db.Dispose();
        }

        /// <summary>
        /// 取得合約PDF檔案
        /// </summary>
        /// <param name="contractNo"></param>
        /// <returns></returns>
        private FileInfo? GetContractFile(string contractNo)
        {
            string folderPath = _KNFileUploadSetting.DownloadFolderPath;
            string prefix = $"{_KNFileUploadSetting.ContractQueueid}_{contractNo}";
            string extension = ".pdf";

            var latestFile = Directory
                .EnumerateFiles(folderPath)
                .Where(file =>
                    Path.GetFileName(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase))
                .Select(file => new FileInfo(file))
                .OrderByDescending(f => f.CreationTime)
                .FirstOrDefault();

            return latestFile;
        }

        /// <summary>
        /// 建立合約
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ContractResultModel CreateContract(ContractModel model)
        {
            using DCDataContext db = new();
            db.Connection.Open();
            db.Transaction = db.Connection.BeginTransaction();
            bool transFail = false;
            try
            {
                // 建立合約
                var (contract, promisor) = CreateContract(db, model);

                // 建立起約者合約簽署要求
                CreateContractSignatureRequest(contract, promisor?.CompanyID ?? 0, promisor?.UID ?? 0, model.IsPassStamp);

                // 建立起約者相關合約公司
                CreateContractingParty(contract, promisor?.CompanyID ?? 0, (int)ContractingIntent.ContractingIntentEnum.Initiator, true);

                // 相關合約公司ID
                HashSet<int> companyCPIds = [];
                HashSet<string> sendMails = [];
                sendMails.Add(model.NotifyMail);
                // 檢查簽屬者公司是否存在
                foreach (var signatory in model.Signatories)
                {
                    // 建立簽署者公司
                    int companyId = CreateOrganization(db, signatory, promisor?.CompanyID ?? 0);
                    // 建立簽署者
                    int uid = CreateUserProfile(db, signatory, companyId);
                    // 建立簽署者合約簽署要求
                    CreateContractSignatureRequest(contract, companyId, uid, model.IsPassStamp);

                    companyCPIds.Add(companyId);
                    sendMails.Add(signatory.Mail);
                }

                // 建立簽屬者相關合約公司
                foreach (var companyId in companyCPIds)
                {
                    CreateContractingParty(contract, companyId, (int)ContractingIntent.ContractingIntentEnum.Contractor, false);
                }

                // 合約建立步驟
                CreateDocumentProcessLog(contract, promisor?.UID ?? 0, model.IsPassStamp);

                db.SubmitChanges();
                

                // 發送簽署通知
                var emailContent = (model.IsPassStamp) ? _emailFactory.GetNotifySign() : _emailFactory.GetNotifySeal();
                var targetUsers = (model.IsPassStamp) ? sendMails.ToList() : [model.NotifyMail];
                SendMail(contract, targetUsers, emailContent);

                db.Transaction.Commit();
            } 
            catch(Exception ex)
            {
                FileLogger.Logger.Error(ex.ToString());
                transFail = true;
                db.Transaction.Rollback();
            }
            finally
            {
                db.Connection.Close();
                db.Dispose();
            }

            return new ContractResultModel
            {
                msgRes = new MsgRes
                {
                    type = (transFail) ? ContractResultType.F.ToString() : ContractResultType.S.ToString(),
                    code = ContractResultCode.ContractCreate.GetFullCode(),
                    desc = $"合約編號({model.ContractNo})-建立" + ((transFail) ? "失敗" : "成功")
                }
            };
        }

        /// <summary>
        /// 驗證合約Model是否正確
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modelState"></param>
        /// <returns></returns>
        public bool IsValid(ContractModel model, ModelStateDictionary modelState, out ContractResultModel result)
        {
            using DCDataContext db = new();

            // 檢查起約人公司是否存在
            int companyId = 0;
            if (!string.IsNullOrEmpty(model.NotifyMail))
            {
                var company = from u in db.UserProfile
                              join ou in db.OrganizationUser on u.UID equals ou.UID
                              where u.EMail.Equals(model.NotifyMail) && ou.CompanyID == 21
                              select new
                              {
                                  ou.CompanyID
                              };
                if (company.FirstOrDefault() == null)
                {
                    modelState.AddModelError(nameof(model.NotifyMail), "通知mail帳號不存在");
                }
                else
                {
                    companyId = company.FirstOrDefault()?.CompanyID ?? 0;
                }
            }

            
            if (!string.IsNullOrEmpty(model.ContractNo) && companyId != 0)
            {
                // 檢查合約編號是否已存在
                var contract = from c in db.Contract
                               where c.ContractNo.Equals(model.ContractNo) && c.CompanyID == companyId
                               select new
                               {
                                   c.ContractID,
                                   c.ContractNo
                               };

                if (contract.FirstOrDefault() != null)
                {
                    modelState.AddModelError(nameof(model.ContractNo), "合約編號已存在");
                }

                // 檢查合約PDF是否存在
                if (GetContractFile(model.ContractNo) == null)
                {
                    modelState.AddModelError(nameof(model.ContractNo), "合約PDF不存在");
                }
            }

            db.Dispose();

            if (!model.Signatories.Any())
            {
                modelState.AddModelError(nameof(model.Signatories), "簽署者至少要一個");
            }

            List<string> errorMsg = [];
            List<string> errorList = [];

            foreach (var signatory in model.Signatories)
            {
                if (errorList.Count == 4)
                {
                    break;
                }

                if (string.IsNullOrEmpty(signatory.Identifier) && !errorList.Contains(nameof(signatory.Identifier)))
                {
                    errorMsg.Add("公司統編為必填");
                    errorList.Add(nameof(signatory.Identifier));
                }
                if (string.IsNullOrEmpty(signatory.Name) && !errorList.Contains(nameof(signatory.Name)))
                {
                    errorMsg.Add("名稱為必填");
                    errorList.Add(nameof(signatory.Name));
                }
                if (string.IsNullOrEmpty(signatory.Mail) && !errorList.Contains(nameof(signatory.Mail)))
                {
                    errorMsg.Add("Mail為必填");
                    errorList.Add(nameof(signatory.Mail));
                }

                string errorKey = nameof(signatory.Mail) + "_AddressValidFail";
                if (!new EmailAddressAttribute().IsValid(signatory.Mail) && !errorList.Contains(errorKey))
                {
                    errorMsg.Add("Mail格式不正確");
                    errorList.Add(errorKey);
                }
            }

            if (errorMsg.Count > 0)
            {
                modelState.AddModelError(nameof(model.Signatories), $"簽署者({string.Join(",", errorMsg)})");
            }

            result = new ContractResultModel()
            {
                msgRes = new MsgRes()
                {
                    type = (!modelState.IsValid) ? ContractResultType.F.ToString() : ContractResultType.S.ToString(),
                    code = ContractResultCode.ContractInfoVerify.GetFullCode(),
                    desc = (!modelState.IsValid) ? modelState.ErrorMessage() : "合約資訊正確"
                }
            };

            return modelState.IsValid;
        }

        /// <summary>
        /// 下載合約
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public async Task<ContractResultModel> DownloadAsync(HttpRequest Request)
        {
            HttpChunkResult chunkResult = new();

            try
            {
                chunkResult = await _httpChunkService.DownloadAsync(Request);
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex.ToString());
                chunkResult.Code = (int)HttpChunkResultCodeEnum.SYSTEM_ERROR;
                chunkResult.Message = ex.ToString();
            }

            if (chunkResult.Code != (int)HttpChunkResultCodeEnum.COMPLETE)
            {
                return new ContractResultModel()
                {
                    msgRes = new MsgRes()
                    {
                        type = ContractResultType.F.ToString(),
                        code = ContractResultCode.ContractDownload.GetFullCode(),
                        desc = chunkResult.Message
                    }
                };
            }

            return new ContractResultModel()
            {
                msgRes = new MsgRes()
                {
                    type = ContractResultType.S.ToString(),
                    code = ContractResultCode.ContractDownload.GetFullCode(),
                    desc = $"合約PDF下載成功-{chunkResult.Message}"
                }
            };
        }
        /// <summary>
        /// 建議簽署合約PDF
        /// </summary>
        /// <param name="contract"></param>
        public async Task<string> CreateSignaturePDF(Contract contract)
        {
            if (contract.ContractSignature == null)
            {
                return string.Empty;
            }

            ContractSignatureRequest request = contract.ContractSignature.ContractSignatureRequest;

            if (request.ResponsePath == null)
            {
                return string.Empty;
            }

            if (!request.RequestPath.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
            {
                return string.Empty;
            }

            JObject content = JObject.Parse(File.ReadAllText(request.ResponsePath));
            string code = content["code"]?.ToString() ?? "";

            if (code != "0")
            {
                return string.Empty;
            }

            string msg = content["msg"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(msg))
            {
                return string.Empty;
            }

            byte[] pdfBytes = Convert.FromBase64String(msg);
            //PdfDocument pdfDocument = new(pdfBytes);
            string fileName = $"{_KNFileUploadSetting.SignatureQueueid}_{contract.ContractNo}_{_KNFileUploadSetting.FileCurrentDateTime}.pdf";
            string saveFilePath = Path.Combine(_KNFileUploadSetting.DownloadFolderPath, fileName);
            // 透過 IronPdf 儲存PDF檔案 會洗掉簽章有效性
            //pdfDocument.SaveAs(saveFilePath);

            await File.WriteAllBytesAsync(saveFilePath, pdfBytes);

            return saveFilePath;
        }

        /// <summary>
        /// 建議簽署軌跡PDF
        /// </summary>
        /// <param name="contract"></param>
        public async Task<string> CreateFootprintsPDF(Contract contract)
        {
            using DCDataContext db = new();

            SignHistoryPager signHistoryPager = new()
            {
                FileNo = contract.ContractNo,
                FileName = contract.Title
            };
            // 取得合約發起人資訊
            var initiatorInfo = (from u in db.UserProfile
                                join ou in db.OrganizationUser on u.UID equals ou.UID
                                join o in db.Organization on ou.CompanyID equals o.CompanyID
                                join dp in db.DocumentProcessLog on u.UID equals dp.ActorID
                                where dp.DocID == contract.ContractID && dp.StepID == (int)CDS_Document.StepEnum.Establish
                                select new
                                {
                                    u.UserName,
                                    u.EMail,
                                    o.CompanyName
                                }).FirstOrDefault();
            // 設定起約人資訊
            if (initiatorInfo != null)
            {
                signHistoryPager.InitiatorName = $"{initiatorInfo.EMail}({initiatorInfo.CompanyName})";
                signHistoryPager.InitiatorMail = initiatorInfo.EMail;
            }

            // 設定簽署軌跡
            signHistoryPager.Histories = from dp in db.DocumentProcessLog
                                        join u in db.UserProfile on dp.ActorID equals u.UID
                                        join ou in db.OrganizationUser on u.UID equals ou.UID
                                        join o in db.Organization on ou.CompanyID equals o.CompanyID
                                        where dp.DocID == contract.ContractID
                                        select new History
                                        {
                                            CompanyName = o.CompanyName,
                                            LogDate = dp.LogDate,
                                            StepID = dp.StepID,
                                            Mail = u.EMail,
                                            IP = dp.ClientIP,
                                            Device = dp.ClientDevice
                                        };
            // 設定建立時間
            signHistoryPager.CreateDateTime = signHistoryPager.Histories
                                             .Where(db => db.StepID == (int)CDS_Document.StepEnum.Establish)
                                             .Select(db => db.LogDate).FirstOrDefault()?.ToString("yyyy/MM/dd HH:mm:ss") ?? "";

            // 設定完成時間
            signHistoryPager.FinishedDateTime = signHistoryPager.Histories
                                             .Where(db => db.StepID == (int)CDS_Document.StepEnum.Committed)
                                             .Select(db => db.LogDate).FirstOrDefault()?.ToString("yyyy/MM/dd HH:mm:ss") ?? "";

            // 設定簽署人
            signHistoryPager.Signers = from cs in db.ContractSignatureRequest
                                       join u in db.UserProfile on cs.SignerID equals u.UID
                                       join ou in db.OrganizationUser on u.UID equals ou.UID
                                       join o in db.Organization on ou.CompanyID equals o.CompanyID
                                       where cs.ContractID == contract.ContractID
                                       select new Signer
                                       {
                                           CompanyName = o.CompanyName,
                                           Name = u.UserName,
                                           Mail = u.EMail,
                                           Region = u.Region
                                       };

            var rptViewRenderString = await _viewRenderService.RenderToStringAsync(
                                      viewName: signHistoryPager.TemplateItem,
                                      model: signHistoryPager);

            var renderer = new ChromePdfRenderer();
            PdfDocument pdfDocument = await renderer.RenderHtmlAsPdfAsync(rptViewRenderString);
            string fileName = $"{_KNFileUploadSetting.HistoryQueueid}_{contract.ContractNo}_{_KNFileUploadSetting.FileCurrentDateTime}.pdf";
            string saveFilePath = Path.Combine(_KNFileUploadSetting.DownloadFolderPath, fileName);
            pdfDocument.SaveAs(saveFilePath);

            return saveFilePath;
        }
        /// <summary>
        /// 上傳簽屬及軌跡PDF檔案
        /// </summary>
        public async Task UploadSignatureAndFootprintsPdfFile(Contract contract)
        {
            var createTasks = new List<Task<string>>
            {
                this.CreateSignaturePDF(contract),
                this.CreateFootprintsPDF(contract)
            };

            var results = await Task.WhenAll(createTasks);
            
            var uploadTasks = new List<Task>();

            foreach (var filePath in results)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    uploadTasks.Add(_chunkFileUploader.UploadAsync(filePath));
                }
            }

            await Task.WhenAll(uploadTasks);
        }
    }
}
