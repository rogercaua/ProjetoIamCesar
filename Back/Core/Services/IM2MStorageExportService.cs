using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public interface IM2MStorageExportService
{
    Task<ExportResultDto> ExportAsync(DocumentRecord document, string clientId);
}
