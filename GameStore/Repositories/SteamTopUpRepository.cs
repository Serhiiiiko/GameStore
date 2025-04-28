using GameStore.Data;
using GameStore.Interfaces;
using GameStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Repositories
{
    public class SteamTopUpRepository : ISteamTopUpRepository
    {
        private readonly ApplicationDbContext _context;

        public SteamTopUpRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SteamTopUp>> GetAllAsync()
        {
            return await _context.SteamTopUps.ToListAsync();
        }

        public async Task<SteamTopUp?> GetByIdAsync(int id)
        {
            return await _context.SteamTopUps.FindAsync(id);
        }

        public async Task<IEnumerable<SteamTopUp>> GetBySteamIdAsync(string steamId)
        {
            return await _context.SteamTopUps
                .Where(s => s.SteamId == steamId)
                .ToListAsync();
        }

        public async Task<SteamTopUp> CreateAsync(SteamTopUp topUp)
        {
            _context.SteamTopUps.Add(topUp);
            await _context.SaveChangesAsync();
            return topUp;
        }

        public async Task UpdateAsync(SteamTopUp topUp)
        {
            _context.Entry(topUp).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}