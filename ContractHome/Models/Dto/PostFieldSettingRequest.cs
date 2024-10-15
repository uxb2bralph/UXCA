using ContractHome.Helper;
using ContractHome.Models.ViewModel;
using FluentValidation;

namespace ContractHome.Models.Dto
{
    public class PostFieldSettingRequest
    {
        public string ContractID { get; set; }
        public IEnumerable<PostFieldSettingRequestFields> FieldSettings { get; set; }
        public string? EncUID { get; set; }

        public class PostFieldSettingRequestFields
        {
            public string? CompanyID { get; set; }
            public string ID { get; set; }
            public double ScaleWidth { get; set; }
            public double ScaleHeight { get; set; }
            public double MarginTop { get; set; }
            public double MarginLeft { get; set; }
            public int PageIndex { get; set; }
            //0:default 1:文字 2.地址 3.電話 4.日期 5.公司Title 6.印章 7.簽名 8.圖片 ... 擴充?
            public int Type { get; set; }
            public string? OperatorID { get; set; }

            public PostFieldSettingRequestFields(string companyID, string iD, double scaleWidth, double scaleHeight, 
                double marginTop, double marginLeft, int pageIndex, int type, string operatorID="")
            {
                CompanyID = companyID;
                ID = iD;
                ScaleWidth = scaleWidth;
                ScaleHeight = scaleHeight;
                MarginTop = marginTop;
                MarginLeft = marginLeft;
                PageIndex = pageIndex;
                Type = type;
                OperatorID = operatorID;
            }
        }

        public class Validator:AbstractValidator<PostFieldSettingRequest>
        {
            public Validator() 
            {
                this.RuleFor(x => x.ContractID)
                    .NotEmpty()
                    .Must(y => GeneralValidator.TryDecryptKeyValue(y));
                RuleFor(req => req.FieldSettings).NotEmpty();
                RuleForEach(req => req.FieldSettings).SetValidator(new FieldItemValidator());
            }

            public class FieldItemValidator : AbstractValidator<PostFieldSettingRequestFields>
            {
                public FieldItemValidator()
                {
                    this.RuleFor(x => x.CompanyID)
                        //.NotEmpty()
                        .Must(y => GeneralValidator.TryDecryptKeyValue(y));

                    this.RuleFor(x => x.ID).NotEmpty();

                    this.RuleFor(x => x.ScaleWidth)
                        .NotNull()
                        .GreaterThanOrEqualTo(0);

                    this.RuleFor(x => x.ScaleHeight)
                        .NotNull()
                        .GreaterThanOrEqualTo(0);

                    this.RuleFor(x => x.MarginTop)
                        .NotNull()
                        .GreaterThanOrEqualTo(0);

                    this.RuleFor(x => x.MarginLeft)
                        .NotNull()
                        .GreaterThanOrEqualTo(0);

                    this.RuleFor(x => x.PageIndex)
                        .NotNull()
                        .GreaterThanOrEqualTo(0);

                    this.RuleFor(x => x.Type)
                        .NotNull();

                    this.RuleFor(x => x.OperatorID)
                        .Must(y => GeneralValidator.TryDecryptKeyValue(y));
                }
            }
        }
    }
}
