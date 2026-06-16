namespace SharedModels.Interfaces;

public interface ICatalogService
{
    // Gifts operations
    Task<IEnumerable<SharedModels.Dtos.DtoGifts>> GetAllGiftsAsync();
    Task<SharedModels.Dtos.DtoGifts?> GetGiftByIdAsync(int giftId);
    Task<IEnumerable<SharedModels.Dtos.DtoGifts>> GetGiftsByFilterAsync(SharedModels.Dtos.DtoGiftFilter filter);
    Task<int> CreateGiftAsync(SharedModels.Dtos.DtoCreateGiftRequest request);
    Task<bool> UpdateGiftAsync(int giftId, SharedModels.Dtos.DtoUpdateGiftRequest request);
    Task<bool> DeleteGiftAsync(int giftId);

    // Donors operations
    Task<IEnumerable<SharedModels.Dtos.DtoDonors>> GetAllDonorsAsync();
    Task<SharedModels.Dtos.DtoDonors?> GetDonorByIdAsync(int donorId);
    Task<int> CreateDonorAsync(SharedModels.Dtos.DtoCreateDonorRequest request);
    Task<bool> UpdateDonorAsync(int donorId, SharedModels.Dtos.DtoUpdateDonorRequest request);
    Task<bool> DeleteDonorAsync(int donorId);

    // Categories operations
    Task<IEnumerable<SharedModels.Dtos.DtoCategories>> GetAllCategoriesAsync();
    Task<SharedModels.Dtos.DtoCategories?> GetCategoryByIdAsync(int categoryId);
    Task<int> CreateCategoryAsync(SharedModels.Dtos.DtoCreateCategoryRequest request);
    Task<bool> UpdateCategoryAsync(int categoryId, SharedModels.Dtos.DtoUpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(int categoryId);
}
