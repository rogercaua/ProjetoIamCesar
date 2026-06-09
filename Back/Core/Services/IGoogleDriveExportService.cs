using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;

namespace DocumentPortalIam.Back.Core.Services;

public interface IGoogleDriveExportService
{
    Task<ExportResultDto> ExportAsync(DocumentRecord document, string actor, string? driveFileName);
}
