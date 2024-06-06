using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Properties;
using DocumentFormat.OpenXml.Spreadsheet;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public class PasswordUpdated : LoginFailed
    {
        public PasswordUpdated(IEmailBodyBuilder emailBodyBuilder) : base(emailBodyBuilder)
        {
        }

        public string Subject => @"[安心簽]密碼更新通知信";
    }
}
