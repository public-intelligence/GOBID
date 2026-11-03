using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Personnel;

public class DmsDocumentUploadResponseDTO
{
    [JsonProperty("documentId")]
    public Guid DocumentId { get; set; }
}
