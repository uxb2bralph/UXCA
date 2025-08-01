using static ContractHome.Services.ContractService.ContractSearchDtos;

namespace ContractHome.Services.ContractService
{
    public interface IContractSearchService
    {
        public ContractListDataModel SearchContract(ContractSearchModel searchModel);
    }
}
