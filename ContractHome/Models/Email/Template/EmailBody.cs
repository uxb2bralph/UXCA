using CommonLib.Core.Utility;
using ContractHome.Models.DataEntity;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using static ContractHome.Models.Email.Template.EmailBody;

namespace ContractHome.Models.Email.Template
{
    public class EmailBody
    {
        private readonly IViewRenderService _viewRenderService;
        public EmailBody(IViewRenderService viewRenderService)
        {
            _viewRenderService = viewRenderService;
        }

        public string ContractNo { get; set; }
        public string Title { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string RecipientUserName { get; set; }
        public string RecipientUserEmail { get; set; }
        public string ContractLink { get; set; }
        public string VerifyLink { get; set; }

        public string TemplateItem { get; set; }

        public enum EmailTemplate
        {
            Undefined = 0,
            NotifySeal = 2, //v2用印
            NotifySign = 5, //v2簽章
            NotifySignature = 6, //v3可視簽章
            FinishContract = 8,
            LoginFailed = 9,
            LoginSuccessed = 10,
            PasswordUpdated = 14,   //密碼變更
            WelcomeUser = 15,      //帳號建立密碼Token申請
            ApplyPassword = 20      //密碼重置Token申請
        }

        public void SetContractNo(string contractno)
        {
            this.ContractNo = contractno;
        }

        public void SetTitle(string title)
        {
            this.Title = title;
        }

        public void SetUserName(string username)
        {
            this.UserName = username;
        }

        public void SetUserEmail(string useremail)
        {
            this.UserEmail = useremail;
        }

        public void SetRecipientUserName(string recipientUserName)
        {
            this.RecipientUserName = recipientUserName;
        }

        public void SetRecipientUserEmail(string recipientUserEmail)
        {
            this.RecipientUserEmail = recipientUserEmail;
        }

        public void SetContractLink(string contractLink)
        {
            this.ContractLink = contractLink;
        }
        public void SetVerifyLink(string verifyLink)
        {
            this.VerifyLink = verifyLink;
        }

        public void SetTemplateItem(string item)
        {
            this.TemplateItem = item;
        }

        public string GetTemplateView()
        {
            if (string.IsNullOrEmpty(this.TemplateItem)) 
            { 
                return string.Empty;
            }

            return @$"~/Views/Shared/EmailTemplate/{this.TemplateItem}.cshtml";
        }

        public async Task<string> GetViewRenderString()
        {
            return await _viewRenderService.RenderToStringAsync(
                viewName: this.GetTemplateView(),
                model: this);
        }
    }
}
