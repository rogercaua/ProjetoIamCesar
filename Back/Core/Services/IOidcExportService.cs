using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public interface IOidcExportService
{
    Task<ExportResultDto> ExportAsync(DocumentRecord document, string userName, string providerAccount);
}
