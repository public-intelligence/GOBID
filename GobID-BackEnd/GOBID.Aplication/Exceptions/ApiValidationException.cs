using FluentValidation.Results;
using System;
using System.Collections.Generic;

namespace GOBID.Aplication.Exceptions
{
    public class ApiValidationException : Exception
    {
        public ApiValidationException() : base("validations error are occurred")
        {
            Errors = new List<string>();
        }
        public List<string> Errors { get; }
        public ApiValidationException(IEnumerable<ValidationFailure> failures) : this()
        {
            foreach (var failure in failures)
            {
                Errors.Add(failure.ErrorMessage);
            }
        }
    }
}
    


