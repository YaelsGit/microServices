using SharedModels.Models;
using SharedModels.Dtos;
using SharedModels.Constants;
using CatalogService.Interfaces;

namespace CatalogService.Services;

/// <summary>
/// Service implementation for Donor business logic
/// Handles CRUD operations and donor-related business rules
/// </summary>
public class DonorsService : IDonorsService
{
    private readonly IDonorsRepository _repository;
    private readonly ILogger<DonorsService> _logger;

    public DonorsService(IDonorsRepository repository, ILogger<DonorsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all donors
    /// </summary>
    public async Task<IEnumerable<DtoDonors>> GetAllDonorsAsync()
    {
        try
        {
            var donors = await _repository.GetAllDonorsAsync();
            return donors.Select(d => MapToDto(d));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all donors");
            throw;
        }
    }

    /// <summary>
    /// Get donor by ID
    /// </summary>
    public async Task<DtoDonors?> GetDonorByIdAsync(int donorId)
    {
        try
        {
            if (donorId <= 0)
                return null;

            var donor = await _repository.GetDonorByIdAsync(donorId);
            return donor == null ? null : MapToDto(donor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting donor: {donorId}");
            throw;
        }
    }

    /// <summary>
    /// Create new donor
    /// </summary>
    public async Task<int> CreateDonorAsync(DtoCreateDonorRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Create donor attempt with empty name");
                throw new ArgumentException("Donor name is required");
            }

            var donor = new Donor
            {
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                City = request.City,
                Country = request.Country
            };

            var donorId = await _repository.CreateDonorAsync(donor);
            _logger.LogInformation($"New donor created: {donorId}");
            return donorId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating donor");
            throw;
        }
    }

    /// <summary>
    /// Update donor
    /// </summary>
    public async Task<bool> UpdateDonorAsync(int donorId, DtoUpdateDonorRequest request)
    {
        try
        {
            var donor = await _repository.GetDonorByIdAsync(donorId);
            if (donor == null)
            {
                _logger.LogWarning($"Update donor attempt for non-existent donor: {donorId}");
                return false;
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.Name))
                donor.Name = request.Name;
            if (!string.IsNullOrWhiteSpace(request.Email))
                donor.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                donor.PhoneNumber = request.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(request.Address))
                donor.Address = request.Address;
            if (!string.IsNullOrWhiteSpace(request.City))
                donor.City = request.City;
            if (!string.IsNullOrWhiteSpace(request.Country))
                donor.Country = request.Country;

            var success = await _repository.UpdateDonorAsync(donor);
            if (success)
            {
                _logger.LogInformation($"Donor updated: {donorId}");
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating donor: {donorId}");
            throw;
        }
    }

    /// <summary>
    /// Delete donor
    /// </summary>
    public async Task<bool> DeleteDonorAsync(int donorId)
    {
        try
        {
            var success = await _repository.DeleteDonorAsync(donorId);
            if (success)
            {
                _logger.LogInformation($"Donor deleted: {donorId}");
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting donor: {donorId}");
            throw;
        }
    }

    /// <summary>
    /// Helper method to map Donor to DtoDTO
    /// </summary>
    private DtoDonors MapToDto(Donor donor)
    {
        return new DtoDonors
        {
            DonorId = donor.DonorId,
            Name = donor.Name,
            Email = donor.Email,
            PhoneNumber = donor.PhoneNumber,
            Address = donor.Address,
            City = donor.City,
            Country = donor.Country,
            CreatedAt = donor.CreatedAt,
            UpdatedAt = donor.UpdatedAt
        };
    }
}
