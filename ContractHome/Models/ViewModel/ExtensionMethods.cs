using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Spreadsheet;
using CommonLib.Utility;

namespace ContractHome.Models.ViewModel
{
    public static class ExtensionMethods
    {
        public static int? GetCompanyID(this UserProfileViewModel viewModel)
        {
            viewModel.EncCompanyID = viewModel.EncCompanyID.GetEfficientString();
            if (viewModel.EncCompanyID != null)
            {
                return viewModel.EncCompanyID.DecryptKeyValue();
            }
            return null;
        }
    }
}
