using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityHub.Domain.Response;
public interface IResult<T> : IResult
{
    /// <summary>
    /// The value of the result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when IsSuccess is false.</exception>
    /// <returns>The value of the result.</returns>
    public T Value { get; }
}