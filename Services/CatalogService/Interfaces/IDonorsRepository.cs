using SharedModels.Models;

namespace CatalogService.Interfaces;

/// <summary>
/// Repository interface for Donor data access in CatalogService
/// </summary>
public interface IDonorsRepository
{
    Task<Donor?> GetDonorByIdAsync(int donorId);
    Task<List<Donor>> GetAllDonorsAsync();
    Task<int> CreateDonorAsync(Donor donor);
    Task<bool> UpdateDonorAsync(Donor donor);
    Task<bool> DeleteDonorAsync(int donorId);
    Task<Donor?> GetDonorByEmailAsync(string email);
    Task<List<Donor>> SearchDonorsAsync(string searchTerm);
}
