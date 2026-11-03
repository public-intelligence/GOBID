using IdentityHub.Domain.DTOs.Error;
using System.Net;

namespace IdentityHub.Domain.Response;

public interface IHttpResult : IResult
{
    public new ErrorDto Error { get; }
    /// <summary>
    /// The HTTP status code of the result.
    /// </summary>
    public HttpStatusCode StatusCode { get; }
}
