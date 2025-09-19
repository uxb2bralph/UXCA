using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using static ContractHome.Models.DataEntity.CDS_Document;
using static ContractHome.Services.ContractService.ContractSearchDtos;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ContractHome.Services.ContractService
{
    public class ContractSearchService(DCDataContext db) : IContractSearchService
    {
        private readonly DCDataContext _db = db;

        /// <summary>
        /// 查詢功能
        /// </summary>
        /// <param name="searchModel"></param>
        /// <returns></returns>
        public ContractListDataModel Search(ContractSearchModel searchModel)
        {
            var query = GetContractMainQuery(searchModel);

            var result = query
                .Skip((searchModel.PageIndex - 1) * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .ToList();

            BuildProcessLog(result);

            BuilldPartyRef(result, searchModel.SearchCompanyID);


            ContractListDataModel contractListDataModel = new()
            {
                Contracts = result,
                TotalRecordCount = query.Count()
            };
             
            return contractListDataModel;
        }

        private IQueryable<ContractInfoMode> GetContractMainQuery(ContractSearchModel searchModel)
        {
            var query = from c in _db.Contract
                        join cc in _db.ContractCategory on c.ContractCategoryID equals cc.ContractCategoryID into ccl
                        from ccu in ccl.DefaultIfEmpty()
                        join cd in _db.CDS_Document on c.ContractID equals cd.DocID
                        where cd.CurrentStep != (int)StepEnum.Initial
                        select new ContractInfoMode
                        {
                            ContractID = c.ContractID,
                            KeyID = c.ContractID.EncryptKey(),
                            ContractNo = c.ContractNo,
                            ContractCategoryID = c.ContractCategoryID,
                            ContractCategory = c.ContractCategoryID.EncryptKey(),
                            CategoryName = (ccu != null) ? ccu.CategoryName : "",
                            Title = c.Title,
                            IsPassStamp = c.IsPassStamp,
                            CurrentStep = (int)cd.CurrentStep,
                            CreatedDateTimeO = cd.DocDate,
                            NotifyUntilDateO = c.NotifyUntilDate,
                        };

            if (!string.IsNullOrEmpty(searchModel.ContractNo))
            {
                query = query.Where(c => c.ContractNo.Contains(searchModel.ContractNo));
            }

            if (searchModel.QueryStep != null && searchModel.QueryStep.Length != 0)
            {
                query = query.Where(c => searchModel.QueryStep.Contains((StepEnum)c.CurrentStep));

            }

            if (!string.IsNullOrEmpty(searchModel.ContractDateFrom))
            {
                DateTime fromDate = DateTime.Parse(searchModel.ContractDateFrom);
                query = query.Where(c => c.CreatedDateTimeO >= fromDate);
            }

            if (!string.IsNullOrEmpty(searchModel.ContractDateTo))
            {
                DateTime toDate = DateTime.Parse(searchModel.ContractDateTo);
                query = query.Where(c => c.CreatedDateTimeO <= toDate.Date.AddDays(1).AddSeconds(-1));
            }

            if (!string.IsNullOrEmpty(searchModel.Initiator))
            {
                var parties = from p in _db.ContractingParty
                              where p.CompanyID == searchModel.InitiatorID
                              where p.IsInitiator == true
                              select p.ContractID;
                query = query.Where(c => parties.Any(p => p == c.ContractID));
            }

            if (!string.IsNullOrEmpty(searchModel.Contractor))
            {
                var parties = from p in _db.ContractingParty
                              where p.CompanyID == searchModel.ContractorID
                              where p.IsInitiator == false
                              select p.ContractID;
                query = query.Where(c => parties.Any(p => p == c.ContractID));
            }

            // 登入者 是否為 合約簽署人
            if (searchModel.SearchUID > 0 || searchModel.SearchCompanyID > 0)
            {
                // 取出有指定簽署人ID的合約ID
                var signContractIDs = from d in _db.ContractSignatureRequest
                                      where d.SignerID == searchModel.SearchUID
                                      select d.ContractID;
                // 只取沒指定簽署人ID的合約
                var unSignContractIDs = from d in _db.ContractSignatureRequest
                                        join c in _db.Contract on d.ContractID equals c.ContractID
                                        where d.SignerID == null && d.CompanyID == searchModel.SearchCompanyID
                                        select d.ContractID;

                // 追加分類條件
                if (searchModel.ContractCategoryID.Count > 0)
                {
                    unSignContractIDs = from d in _db.ContractSignatureRequest
                                        join c in _db.Contract on d.ContractID equals c.ContractID
                                        where d.SignerID == null && d.CompanyID == searchModel.SearchCompanyID
                                        && searchModel.ContractCategoryID.Contains(c.ContractCategoryID)
                                        && c.ContractCategoryID != 0
                                        select d.ContractID;
                }

                // 合併兩個查詢結果
                signContractIDs = signContractIDs.Union(unSignContractIDs);

                // 根據 WaittingStepEnum 過濾合約ID
                var resultContractIDs = GetWaittingContractQuery(signContractIDs, searchModel);

                query = query.Where(c => resultContractIDs.Any(s => s == c.ContractID));
            }

            // 排序
            if (searchModel.SortName.Any() && searchModel.SortType.Any())
            {
                for (int i = 0; i < searchModel.SortName.Length; i++)
                {
                    var sortName = searchModel.SortName[i];
                    var sortType = searchModel.SortType[i];

                    query = query.OrderByDynamic(sortName, (sortType > 0));
                }
            }
            else
            {
                // 預設排序
                query = query.OrderByDescending(c => c.CreatedDateTimeO);
            }

            return query;
        }

        private IQueryable<int> GetWaittingContractQuery(IQueryable<int> signContractIDs, ContractSearchModel searchModel)
        {
            if (searchModel.WaittingStepEnum == WaittingStepEnum.MyStamp)
            {
                // 找出待自己用印合約
                var stealingContractIDs = from d in _db.ContractSignatureRequest
                                          where signContractIDs.Any(s => s == d.ContractID) &&
                                          d.CompanyID == searchModel.SearchCompanyID &&
                                          d.StampDate == null &&
                                          d.SignatureDate == null
                                          select d.ContractID;

                return stealingContractIDs;
            }

            if (searchModel.WaittingStepEnum == WaittingStepEnum.MySignature)
            {
                // 找出待自己簽署合約
                var signingContractIDs = from d in _db.ContractSignatureRequest
                                         join c in _db.Contract on d.ContractID equals c.ContractID
                                         where signContractIDs.Any(s => s == d.ContractID) &&
                                         d.CompanyID == searchModel.SearchCompanyID &&
                                         ((c.IsPassStamp == true && d.SignatureDate == null) ||
                                          (c.IsPassStamp == false && d.StampDate != null && d.SignatureDate == null))
                                         select d.ContractID;

                return signingContractIDs;
            }

            if (searchModel.WaittingStepEnum == WaittingStepEnum.CounterpartyStamp)
            {
                // 找出待他人用印合約
                var counterpartyStampContractIDs = from d in _db.ContractSignatureRequest
                                                   where signContractIDs.Any(s => s == d.ContractID) &&
                                                   d.CompanyID != searchModel.SearchCompanyID &&
                                                   d.StampDate == null &&
                                                   d.SignatureDate == null
                                                   select d.ContractID;

                return counterpartyStampContractIDs;
            }


            if (searchModel.WaittingStepEnum == WaittingStepEnum.CounterpartySignature)
            {
                // 找出待他人簽署合約
                var counterpartySignContractIDs = from d in _db.ContractSignatureRequest
                                                  join c in _db.Contract on d.ContractID equals c.ContractID
                                                  where signContractIDs.Any(s => s == d.ContractID) &&
                                                  d.CompanyID != searchModel.SearchCompanyID &&
                                                  ((c.IsPassStamp == true && d.SignatureDate == null) ||
                                                   (c.IsPassStamp == false && d.StampDate != null && d.SignatureDate == null))
                                                  select d.ContractID;

                return counterpartySignContractIDs;
            }

            return signContractIDs;
        }

        /// <summary>
        /// 綁定 DocumentProcessLog 到 ContractInfoMode
        /// </summary>
        /// <param name="db"></param>
        /// <param name="contractInfoModes"></param>
        private void BuildProcessLog(IEnumerable<ContractInfoMode> contractInfoModes)
        {
            // 取的目前合約ID集合
            var contractIDs = contractInfoModes.Select(c => c.ContractID).ToList();

            var dblogs = (from dp in _db.DocumentProcessLog
                          join user in _db.UserProfile on dp.ActorID equals user.UID
                          where contractIDs.Contains(dp.DocID)
                          select new
                          {
                              dp.DocID,
                              dp.LogDate,
                              dp.StepID,
                              user.PID
                          }
                        ).ToList();


            // 處理 DocumentProcessLog 群組
            var dpGroup = dblogs
                        .GroupBy(item => item.DocID)
                        .Select(grouped => new
                        {
                            DocID = grouped.Key,
                            Logs = grouped
                                .OrderBy(x => x.LogDate)
                                .Select(x => new
                                {
                                    x.LogDate,
                                    x.StepID,
                                    x.PID
                                })
                                .ToList()
                        })
                        .ToList();

            // 將 DocumentProcessLog 賦值給每個合約
            foreach (var contract in contractInfoModes)
            {
                var logs = dpGroup.FirstOrDefault(dp => dp.DocID == contract.ContractID);
                if (logs != null)
                {
                    contract.ProcessLogs = logs.Logs.Select(log => new ProcessLog
                    {
                        Time = log.LogDate.ReportDateTimeString(),
                        Action = CDS_Document.StepNaming[log.StepID],
                        Role = log.PID
                    });
                }
            }

        }

        /// <summary>
        /// 綁定 ContractingParty 的 ContractSignatureRequest 資訊到 ContractInfoMode
        /// </summary>
        /// <param name="db"></param>
        /// <param name="contractInfoModes"></param>
        /// <param name="currentCompanyID"></param>
        private void BuilldPartyRef(IEnumerable<ContractInfoMode> contractInfoModes, int currentCompanyID)
        {
            // 取的目前合約ID集合
            var contractIDs = contractInfoModes.Select(c => c.ContractID).ToList();

            // 取出 合約的 甲方 乙方 簽約資訊 未指定簽署人
            var unSingercsr = from c in _db.ContractSignatureRequest
                              join o in _db.Organization on c.CompanyID equals o.CompanyID
                              join p in _db.ContractingParty on new { c.ContractID, c.CompanyID } equals new { p.ContractID, p.CompanyID }
                              where contractIDs.Contains(c.ContractID) && c.SignerID == null
                              select new
                              {
                                  c.ContractID,
                                  c.SignerID,
                                  c.CompanyID,
                                  c.SignatureDate,
                                  c.StampDate,
                                  o.CompanyName,
                                  o.ReceiptNo,
                                  PID = "",
                                  p.IsInitiator,
                                  email = ""
                              };

            // 取出 合約的 甲方 乙方 簽約資訊 指定簽署人
            var singercsr = from c in _db.ContractSignatureRequest
                            join u in _db.UserProfile on c.SignerID equals u.UID
                            join ou in _db.OrganizationUser on u.UID equals ou.UID
                            join o in _db.Organization on ou.CompanyID equals o.CompanyID
                            join p in _db.ContractingParty on new { c.ContractID, c.CompanyID } equals new { p.ContractID, p.CompanyID }
                            where contractIDs.Contains(c.ContractID)
                            select new
                            {
                                c.ContractID,
                                c.SignerID,
                                c.CompanyID,
                                c.SignatureDate,
                                c.StampDate,
                                o.CompanyName,
                                o.ReceiptNo,
                                u.PID,
                                p.IsInitiator,
                                email = u.EMail
                            };

            // 合併兩個簽約資訊
            var csr = unSingercsr.Union(singercsr).ToList();

            // 處理 ContractSignatureRequest 群組
            var csrGroup = csr
                           .GroupBy(item => item.ContractID)
                           .Select(grouped => new
                           {
                               ContractID = grouped.Key,
                               Signers = grouped.OrderByDescending(x => x.IsInitiator)
                                .Select(x => new
                                {
                                    x.ContractID,
                                    x.SignerID,
                                    x.CompanyID,
                                    x.CompanyName,
                                    x.ReceiptNo,
                                    x.SignatureDate,
                                    x.StampDate,
                                    x.PID,
                                    x.IsInitiator,
                                    x.email
                                }
                                )
                                .ToList()
                           })
                           .ToList();

            // 將簽約資訊賦值給每個合約
            foreach (var contract in contractInfoModes)
            {
                var crs = csrGroup.FirstOrDefault(c => c.ContractID == contract.ContractID);
                if (crs != null)
                {
                    contract.Parties = crs.Signers.Select(s => new PartyRefs
                    {
                        KeyID = s.CompanyID.EncryptKey(),
                        ContractID = s.ContractID,
                        SignerID = s.PID,
                        CompanyName = $"{s.CompanyName} ({s.ReceiptNo})" + ((string.IsNullOrEmpty(s.email)) ? "" : " " + s.email),
                        StampDate = s.StampDate?.ToString("yyyy/MM/dd HH:mm") ?? string.Empty,
                        SignerDate = s.SignatureDate?.ToString("yyyy/MM/dd HH:mm") ?? string.Empty,
                        Step = GetPartyRefStep(contract.CurrentStep ?? 0, contract.IsPassStamp, s.IsInitiator, s.StampDate, s.SignatureDate),
                        IsCurrentUserCompany = (s.CompanyID == currentCompanyID),
                        IsInitiator = s.IsInitiator
                    });
                }
            }

            
        }

        private int GetPartyRefStep(int currentStep, bool? isPassStamp, bool? isInitiator, DateTime? stampDate, DateTime? signerDate)
        {
            int step = currentStep;

            // 假如合約在設定狀態 則設定甲方人員為設定狀態
            if (isInitiator == true && currentStep == (int)StepEnum.Config)
            {
                return (int)StepEnum.Config;
            }

            if (isPassStamp == true && !signerDate.HasValue)
            {
                return (int)StepEnum.DigitalSigning;
            }

            if (isPassStamp == false)
            {
                // 未用印 未簽署
                if (!stampDate.HasValue && !signerDate.HasValue)
                {
                    return (int)StepEnum.Sealing;
                }

                // 已用印 未簽署
                if (stampDate.HasValue && !signerDate.HasValue)
                {
                    return (int)StepEnum.DigitalSigning;
                }

                // 已用印 已簽署
                if (stampDate.HasValue && signerDate.HasValue)
                {
                    return (int)StepEnum.DigitalSigned;
                }
            }

            return step;
        }

        public ContractListDataModel AllContract(ContractSearchModel searchModel, UserProfile profile)
        {
            if (profile.IsSysAdmin)
            {
                return Search(searchModel);
            }

            searchModel.SearchUID = profile.UID;
            searchModel.SearchCompanyID = profile.UserCompanyID;
            // 一般使用者追加分類條件
            if (!profile.IsMemberAdmin)
            {
                searchModel.ContractCategoryID = (searchModel.ContractCategoryID.Count > 0) ?
                                                 [.. searchModel.ContractCategoryID.Intersect(profile.CategoryPermission)]
                                                 : profile.CategoryPermission;
            }

            return Search(searchModel);
        }

        public ContractListDataModel WaittingContract(ContractSearchModel searchModel, UserProfile profile)
        {
            if (!searchModel.WaittingStepEnum.HasValue)
            {
                searchModel.WaittingStepEnum = WaittingStepEnum.MyStamp;
            }

            searchModel.QueryStep = CDS_Document.PendingState;
            searchModel.SearchUID = profile.UID;
            searchModel.SearchCompanyID = profile.UserCompanyID;
            searchModel.ContractCategoryID = (searchModel.ContractCategoryID.Count > 0) ?
                                             [.. searchModel.ContractCategoryID.Intersect(profile.CategoryPermission)]
                                             : profile.CategoryPermission;

            return Search(searchModel);
        }
    }
}
