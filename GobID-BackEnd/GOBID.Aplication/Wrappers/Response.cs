using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GOBID.Application.Wrappers
{
    public class Response<T>
    {
        public Response()
        {

        }
        public Response([AllowNull] T data, string? message = null)
        {
            Succeeded = true;
            Message = message;
            Data = data!;
        }

        public Response(string message, bool succeeded)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public T Data { get; set; } = default!;

        /// <summary>
        /// Código HTTP sugerido. Si es null, el controlador usa su lógica por defecto.
        /// </summary>
        public int? StatusCode { get; set; }
    }
}
