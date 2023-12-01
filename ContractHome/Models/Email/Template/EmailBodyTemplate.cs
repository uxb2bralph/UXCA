using CommonLib.Core.Utility;
using Microsoft.IdentityModel.Tokens;

namespace ContractHome.Models.Email.Template
{
    public class EmailBodyTemplate
    {
        private readonly IViewRenderService _viewRenderService;
        public EmailBodyTemplate(IViewRenderService viewRenderService)
        {
            _viewRenderService = viewRenderService;
        }

        public string _contractNo { get; set; }
        public string _title { get; set; }
        public string _userName { get; set; }
        public string _userEmail { get; set; }
        public string _recipientUserName { get; set; }
        public string _recipientUserEmail { get; set; }
        public string _contractLink { get; set; }
        public string _verifyLink { get; set; }
        public string _templateItem { get; set; }

        public void SetContractNo(string contractno)
        {
            this._contractNo = contractno;
        }

        public void SetTitle(string title)
        {
            this._title = title;
        }

        public void SetUserName(string username)
        {
            this._userName = username;
        }

        public void SetUserEmail(string useremail)
        {
            this._userEmail = useremail;
        }

        public void SetRecipientUserName(string recipientUserName)
        {
            this._recipientUserName = recipientUserName;
        }

        public void SetRecipientUserEmail(string recipientUserEmail)
        {
            this._recipientUserEmail = recipientUserEmail;
        }

        public void SetContractLink(string contractLink)
        {
            this._contractLink = contractLink;
        }
        public void SetVerifyLink(string verifyLink)
        {
            this._verifyLink = verifyLink;
        }

        public void SetTemplateItem(string templateitem)
        {
            this._templateItem = templateitem;
        }

        public string GetTemplateView()
        {
            if (this._templateItem.IsNullOrEmpty()) 
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
