using Microsoft.EntityFrameworkCore;
using SharedModels.Models;
using CatalogService.Data;
using CatalogService.Interfaces;

namespace CatalogService.Repository;

/// <summary>
/// Repository implementation for Donor data access
/// Handles CRUD operations on Donors table in CatalogDbContext
/// </summary>
public class DonorsRepository : IDonorsRepository
{
    private readonly CatalogDbContext _context;

    public DonorsRepository(CatalogDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get donor by ID
    /// </summary>
    public async Task<Donor?> GetDonorByIdAsync(int donorId)
    {
        return await _context.Donors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DonorId == donorId);
    }

    /// <summary>
    /// Get all donors
    /// </summary>
    public async Task<List<Donor>> GetAllDonorsAsync()
    {
        return await _context.Donors
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create new donor
    /// Returns the generated DonorId
    /// </summary>
    public async Task<int> CreateDonorAsync(Donor donor)
    {
        donor.CreatedAt = DateTime.UtcNow;
        
        _context.Donors.Add(donor);
        await _context.SaveChangesAsync();
        
        return donor.DonorId;
    }

    /// <summary>
    /// Update existing donor
    /// </summary>
    public async Task<bool> UpdateDonorAsync(Donor donor)
    {
        donor.UpdatedAt = DateTime.UtcNow;
        
        _context.Donors.Update(donor);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Delete donor by ID
    /// </summary>
    public async Task<bool> DeleteDonorAsync(int donorId)
    {
        var donor = await _context.Donors.FindAsync(donorId);
        if (donor == null)
            return false;

        _context.Donors.Remove(donor);
        var rowsAffected = await _context.SaveChangesAsync();
        
        return rowsAffected > 0;
    }

    /// <summary>
    /// Get donor by email address
    /// </summary>
    public async Task<Donor?> GetDonorByEmailAsync(string email)
    {
        return await _context.Donors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Email == email);
    }

    /// <summary>
    /// Search donors by name or email
    /// </summary>
    public async Task<List<Donor>> SearchDonorsAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        
        return await _context.Donors
            .AsNoTracking()
            .Where(d => d.Name.ToLower().Contains(term) || 
                        d.Email.ToLower().Contains(term) ||
                        d.City.ToLower().Contains(term))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
