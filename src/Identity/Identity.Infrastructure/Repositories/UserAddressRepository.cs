using Identity.Application.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class UserAddressRepository : IUserAddressRepository
{
    private readonly IdentityDbContext _context;

    public UserAddressRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public Task<List<SavedAddress>> GetByUserIdAsync(Guid userId)
    {
        return _context.SavedAddresses
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.Label)
            .ToListAsync();
    }

    public Task<SavedAddress?> GetByIdAsync(Guid userId, Guid addressId)
    {
        return _context.SavedAddresses
            .FirstOrDefaultAsync(address => address.UserId == userId && address.Id == addressId);
    }

    public async Task AddAsync(SavedAddress address)
    {
        await _context.SavedAddresses.AddAsync(address);
    }

    public void Remove(SavedAddress address)
    {
        _context.SavedAddresses.Remove(address);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
