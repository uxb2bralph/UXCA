using CommonLib.Core.Utility;
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
        [DefaultValue(EmailTemplate.Undefined)]
        public EmailTemplate _templateItem { get; set; }

        public enum EmailTemplate
        {
            Undefined = 0,
            NotifySeal = 2,
            NotifySign = 5
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

        public void SetTemplateItem(EmailTemplate emailTemplate)
        {
            this._templateItem = emailTemplate;
        }

        public string GetTemplateView()
        {
            if (this._templateItem== EmailTemplate.Undefined) 
            { 
                return string.Empty;
            }

            return @$"~/Views/Shared/EmailTemplate/{this._templateItem}.cshtml";
        }

        public async Task<string> GetViewRenderString()
        {
            return await _viewRenderService.RenderToStringAsync(
                viewName: this.GetTemplateView(),
                model: this);
        }
    }
}
