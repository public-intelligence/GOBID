using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.Wrappers;

namespace GOBID.Infrastructure.Services;

/// <summary>
/// Búsqueda por término en Identity Hub restringida a un conjunto de personas. Identity Hub acepta como
/// máximo 100 resultados por página (PersonApiService.SearchPersonsAsync cae a 50 si se pide más), así que
/// los ids se envían en lotes de 100: en cada lote caben todas sus coincidencias y ninguna se pierde.
/// </summary>
public static class PersonSearchBatching
{
    public const int MaxBatchSize = 100;

    public static async Task<Dictionary<Guid, IdentityPersonResponseDTO>> SearchAllAsync(
        IReadOnlyCollection<Guid> ids,
        Func<List<Guid>, Task<PagedResponse<IdentityPersonResponseDTO>>> searchBatch)
    {
        var matches = new Dictionary<Guid, IdentityPersonResponseDTO>();
        if (ids.Count == 0)
            return matches;

        var wanted = ids.ToHashSet();
        foreach (var batch in ids.Chunk(MaxBatchSize))
        {
            var result = await searchBatch(batch.ToList());
            if (!result.Succeeded || result.Data is null)
                continue;

            foreach (var person in result.Data.Where(p => wanted.Contains(p.Id)))
                matches[person.Id] = person;
        }

        return matches;
    }
}
