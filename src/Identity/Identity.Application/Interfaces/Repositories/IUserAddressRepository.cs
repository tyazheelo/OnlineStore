using Identity.Domain.Entities;

namespace Identity.Application.Interfaces.Repositories;

public interface IUserAddressRepository
{
    Task<List<SavedAddress>> GetByUserIdAsync(Guid userId);
    Task<SavedAddress?> GetByIdAsync(Guid userId, Guid addressId);
    Task AddAsync(SavedAddress address);
    void Remove(SavedAddress address);
    Task SaveChangesAsync();
}
