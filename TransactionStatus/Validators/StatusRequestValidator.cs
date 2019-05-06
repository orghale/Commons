﻿using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransactionStatus.Entities;

namespace TransactionStatus.Validators
{
    public class StatusRequestValidator : AbstractValidator<StatusRequest>
    {
        public StatusRequestValidator()
        {

            RuleFor(req => req.transactionRef)
                    .NotNull()
                    .NotEmpty();
                    //.MaximumLength(100);
        }
    }
}