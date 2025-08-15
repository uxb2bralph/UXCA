using ContractHome.Models.DataEntity;
using static ContractHome.Services.ContractService.ContractSearchDtos;

namespace ContractHome.Services.ContractService
{
    public interface IContractSearchService
    {
        public ContractListDataModel Search(ContractSearchModel searchModel);

        public ContractListDataModel AllContract(ContractSearchModel searchModel, UserProfile userProfile);

        public ContractListDataModel WaittingContract(ContractSearchModel searchModel, UserProfile userProfile);
    }
}
