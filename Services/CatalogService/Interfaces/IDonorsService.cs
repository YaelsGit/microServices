using SharedModels.Dtos;

namespace CatalogService.Interfaces;

/// <summary>
/// Service interface for Donor business logic
/// Defines contracts for donor operations
/// </summary>
public interface IDonorsService
{
    Task<IEnumerable<DtoDonors>> GetAllDonorsAsync();
    Task<DtoDonors?> GetDonorByIdAsync(int donorId);
    Task<int> CreateDonorAsync(DtoCreateDonorRequest request);
    Task<bool> UpdateDonorAsync(int donorId, DtoUpdateDonorRequest request);
    Task<bool> DeleteDonorAsync(int donorId);
}
