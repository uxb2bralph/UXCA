using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Diagnostics.Contracts;
using static ContractHome.Models.DataEntity.CDS_Document;
using static ContractHome.Services.ContractService.ContractSearchDtos;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ContractHome.Services.ContractService
{
    public class ContractSearchService : IContractSearchService
    {
        /// <summary>
        /// 查詢功能
        /// </summary>
        /// <param name="searchModel"></param>
        /// <returns></returns>
        public ContractListDataModel Search(ContractSearchModel searchModel)
        {
            using DCDataContext db = new();

            var query = GetContractMainQuery(db, searchModel);

            var result = query
                .Skip((searchModel.PageIndex - 1) * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .ToList();

            BuildProcessLog(db, result);

            BuilldPartyRef(db, result, searchModel.SearchCompanyID);


            ContractListDataModel contractListDataModel = new()
            {
                Contracts = result,
                TotalRecordCount = query.Count()
            };
             
            return contractListDataModel;
        }

        private IQueryable<ContractInfoMode> GetContractMainQuery(DCDataContext db, ContractSearchModel searchModel)
        {
            var query = from c in db.Contract
                        join cc in db.ContractCategory on c.ContractCategoryID equals cc.ContractCategoryID into ccl
                        from ccu in ccl.DefaultIfEmpty()
                        join cd in db.CDS_Document on c.ContractID equals cd.DocID
                        where cd.CurrentStep != (int)CDS_Document.StepEnum.Initial
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
                query = query.Where(c => searchModel.QueryStep.Contains((CDS_Document.StepEnum)c.CurrentStep));

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
                var parties = from p in db.ContractingParty
                              where p.CompanyID == searchModel.InitiatorID
                              where p.IsInitiator == true
                              select p.ContractID;
                query = query.Where(c => parties.Any(p => p == c.ContractID));
            }

            if (!string.IsNullOrEmpty(searchModel.Contractor))
            {
                var parties = from p in db.ContractingParty
                              where p.CompanyID == searchModel.ContractorID
                              where p.IsInitiator == false
                              select p.ContractID;
                query = query.Where(c => parties.Any(p => p == c.ContractID));
            }

            // 是否為 合約簽署人
            if (searchModel.SearchUID > 0 || searchModel.SearchCompanyID > 0)
            {
                // 取出有指定簽署人ID的合約ID
                var signContractIDs = from d in db.ContractSignatureRequest
                                      where d.SignerID == searchModel.SearchUID
                                      select d.ContractID;
                // 只取沒指定簽署人ID的合約
                var unSignContractIDs = from d in db.ContractSignatureRequest
                                        where d.SignerID == null && d.CompanyID == searchModel.SearchCompanyID
                                        select d.ContractID;

                // 合併兩個查詢結果
                signContractIDs = signContractIDs.Union(unSignContractIDs);

                query = query.Where(c => signContractIDs.Any(s => s == c.ContractID));
            }

            // 授權分類
            if (searchModel.ContractCategoryID.Count > 0)
            {
                query = query.Where(c => searchModel.ContractCategoryID.Contains(c.ContractCategoryID));
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

        /// <summary>
        /// 綁定 DocumentProcessLog 到 ContractInfoMode
        /// </summary>
        /// <param name="db"></param>
        /// <param name="contractInfoModes"></param>
        private void BuildProcessLog(DCDataContext db, IEnumerable<ContractInfoMode> contractInfoModes)
        {
            // 取的目前合約ID集合
            var contractIDs = contractInfoModes.Select(c => c.ContractID).ToList();

            var dblogs = (from dp in db.DocumentProcessLog
                          join user in db.UserProfile on dp.ActorID equals user.UID
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
        private void BuilldPartyRef(DCDataContext db, IEnumerable<ContractInfoMode> contractInfoModes, int currentCompanyID)
        {
            // 取的目前合約ID集合
            var contractIDs = contractInfoModes.Select(c => c.ContractID).ToList();

            // 取出 合約的 甲方 乙方 簽約資訊 未指定簽署人
            var unSingercsr = from c in db.ContractSignatureRequest
                              join o in db.Organization on c.CompanyID equals o.CompanyID
                              join p in db.ContractingParty on new { c.ContractID, c.CompanyID } equals new { p.ContractID, p.CompanyID }
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
                                  p.IsInitiator
                              };

            // 取出 合約的 甲方 乙方 簽約資訊 指定簽署人
            var singercsr = from c in db.ContractSignatureRequest
                            join u in db.UserProfile on c.SignerID equals u.UID
                            join ou in db.OrganizationUser on u.UID equals ou.UID
                            join o in db.Organization on ou.CompanyID equals o.CompanyID
                            join p in db.ContractingParty on new { c.ContractID, c.CompanyID } equals new { p.ContractID, p.CompanyID }
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
                                p.IsInitiator
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
                                    x.IsInitiator
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
                        CompanyName = $"{s.CompanyName} ({s.ReceiptNo})",
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

            if (isPassStamp.HasValue && !isPassStamp.Value)
            {
                // 先用印後簽署 判斷用印跟簽屬時間 設定 用印中 或 簽署中

                // 未用印 未簽署
                if (!stampDate.HasValue && !signerDate.HasValue)
                {
                    step = (int)StepEnum.Sealing;
                }

                // 已用印 未簽署
                if (stampDate.HasValue && !signerDate.HasValue)
                {
                    step = (int)StepEnum.DigitalSigning;
                }

                // 已用印 已簽署
                if (stampDate.HasValue && signerDate.HasValue)
                {
                    step = (int)StepEnum.DigitalSigned;
                }

            }
            else
            {
                // 跳過用印做簽署 判斷簽署時間
                if (!signerDate.HasValue)
                {
                    step = (int)StepEnum.DigitalSigning;
                }
            }

            // 假如合約在設定狀態 則設定甲方人員為設定狀態
            if (isInitiator.HasValue && isInitiator.Value && currentStep == (int)CDS_Document.StepEnum.Config)
            {
                step = (int)CDS_Document.StepEnum.Config;
            }

            return step;
        }
    }
}
