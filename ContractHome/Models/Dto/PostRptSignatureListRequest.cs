﻿using ContractHome.Controllers;
using ContractHome.Helper;
using FluentValidation;
using System.Globalization;

namespace ContractHome.Models.Dto
{
    public class PostRptSignatureListRequest
    {

        public string? QueryDateFromString { get; set; }
        public string? QueryDateEndString { get; set; }
        public string? CompanyID { get; set;}


        public class Validator : AbstractValidator<PostRptSignatureListRequest>
        {
            public Validator()
            {
                //this.When(x => (!string.IsNullOrEmpty(x.CompanyID)), () => {
                //    this.RuleFor(x => GeneralValidator.TryDecryptKeyValue(x.CompanyID));
                //});

                this.RuleFor(x => x.QueryDateFromString)
                    .Must(y => (string.IsNullOrEmpty(y)) ? true : (y.ConvertToDateTime("yyyy/MM/dd") != DateTime.MinValue));

                this.RuleFor(x => x.QueryDateEndString)
                    .Must(y => (string.IsNullOrEmpty(y)) ? true : (y.ConvertToDateTime("yyyy/MM/dd") != DateTime.MinValue));
            }
        }
    }
}