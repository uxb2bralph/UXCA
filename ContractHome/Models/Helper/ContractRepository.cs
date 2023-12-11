using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using static ContractHome.Models.Helper.ContractRepository;

namespace ContractHome.Models.Helper
{
    public class ContractRepository
    {
        protected internal GenericManager<DCDataContext> _models;
        protected internal Contract? _contract;
        public ContractRepository(GenericManager<DCDataContext> models, int? contractID) 
        {
            _models = models;
            _contract = GetContractByID(contractID);
        }

        public Contract? GetContract => _contract;

        public Contract? GetContractByID(int? contractID)
        {
            return _models.GetTable<Contract>()
                    .Where(c => c.ContractID == contractID)
                    .FirstOrDefault();
        }

        //利用原有合約資料新增合約, for非聯合承攬用, 各別成立合約用
        //同時新增CDS_Document及Contract資料
        public Contract? CreateAndSaveContractByOld(int initiatorID)
        {
            var doc = _models.GetTable<CDS_Document>()
                .Where(d => d.DocID == _contract.ContractID).First();

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
                doc!.Contract.ContractSealRequest = null;
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
            SignaturePosition[] SignaturePositions,
            int uid)
        {
            if (_contract == null) { return null; };

            #region 新增ContractingParty
            //移到[建立合約]下一洞新增ContractingParty,for甲方進行[聯合承攬]設定用印位置, 再進入[編輯合約]
            if (!_contract.ContractingParty.Where(p => p.CompanyID == initiatorID)
                .Where(p => p.IntentID == (int)ContractingIntent.ContractingIntentEnum.Initiator)
                .Any())
            {
                _contract.ContractingParty.Add(new ContractingParty
                {
                    CompanyID = initiatorID,
                    IntentID = (int)ContractingIntent.ContractingIntentEnum.Initiator,
                    IsInitiator = true,
                });
            }

            if (!_contract.ContractingParty.Where(p => p.CompanyID == contractorID)
                                .Where(p => p.IntentID == (int)ContractingIntent.ContractingIntentEnum.Contractor)
                                .Any())
            {
                _contract.ContractingParty.Add(new ContractingParty
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

                if (!_contract.ContractSignaturePositionRequest
                    .Where(p => p.ContractID == _contract.ContractID)
                    .Where(p => p.ContractorID == contractorID)
                    .Where(p => p.PositionID == pos.ID)
                .Any())
                {
                    _contract.ContractSignaturePositionRequest.Add(new ContractSignaturePositionRequest
                    {
                        ContractID = _contract.ContractID,
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

            if (!_contract.ContractSignatureRequest.Any(r => r.CompanyID == initiatorID))
            {
                _contract.ContractSignatureRequest.Add(new ContractSignatureRequest
                {
                    CompanyID = initiatorID,
                    StampDate = DateTime.Now,
                });
            }

            if (!_contract.ContractSignatureRequest.Any(r => r.CompanyID == contractorID))
            {
                _contract.ContractSignatureRequest.Add(new ContractSignatureRequest
                {
                    CompanyID = contractorID,
                });
            }
            #endregion

            _models.SubmitChanges();

            _contract.CDS_Document.TransitStep(_models, uid, CDS_Document.StepEnum.InitiatorSealed);
            return _contract;
        }

        public class Contractor
        { 
            public IEnumerable<ContractSignaturePositionResponse>? contractSignaturePositions { get; set; }

            public Contractor(List<ContractSignaturePositionRequest>? 
                contractSignaturePositionRequests)
            {
                //原ContractSignaturePositionRequest沒有constructor, 也沒有get;set;且不是public, 無法用Newtonsoft.JsonConvert
                this.contractSignaturePositions = contractSignaturePositionRequests?
                    .Select(x => new ContractSignaturePositionResponse(
                        requestID: x.RequestID, 
                        contract: x.ContractID.EncryptKey(),
                        contractor: x.ContractorID.EncryptKey(), 
                        positionID: x.PositionID, 
                        scaleWidth:x.ScaleWidth!.Value, scaleHeight:x.ScaleHeight!.Value,
                        marginTop:x.MarginTop!.Value, marginLeft:x.MarginLeft!.Value, 
                        type:x.Type!.Value, pageIndex:x.PageIndex!.Value));
            }
        }

        public class ContractSignaturePositionResponse
        {
            [JsonIgnore]
            public int RequestID { get; set; }
            public string Contract { get; set; }
            public string Contractor { get; set; }
            public string PositionID { get; set; }

            public double ScaleWidth { get; set; }

            public double ScaleHeight { get; set; }

            public double MarginTop { get; set; }

            public double MarginLeft { get; set; }

            public int Type { get; set; }

            public int PageIndex { get; set; }


            public ContractSignaturePositionResponse(int requestID, string contract,
                string contractor, string positionID, double scaleWidth, double scaleHeight, 
                double marginTop, double marginLeft, int type, int pageIndex)

                {
                RequestID = requestID;
                Contract = contract;
                Contractor = contractor;
                PositionID = positionID;
                ScaleWidth = scaleWidth;
                ScaleHeight = scaleHeight;
                MarginTop = marginTop;
                MarginLeft = marginLeft;
                Type = type;
                PageIndex = pageIndex;
            }
        }

        public Contractor GetContractor(int contractorID)
        {
            if (_contract == null) 
            {
                throw new ArgumentNullException();
            }


            var signaturePositions = 
                _contract
                .ContractSignaturePositionRequest.Where(x => x.ContractorID == contractorID)
                .ToList();
            Contractor contractor = new Contractor(signaturePositions);

            //_contract
            //    .ContractSignatureRequest.Where(x => x.CompanyID == contractorID);

            return contractor;
        }

        public Contract? SaveContract()
        {
            _models.SubmitChanges();
            _models.Dispose();
            return _contract;
        }
    }
}
