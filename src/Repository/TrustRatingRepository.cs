using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.Data;
using MarketTrustAPI.Dtos.TrustRating;
using MarketTrustAPI.Interfaces;
using MarketTrustAPI.Models;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.EntityFrameworkCore;

namespace MarketTrustAPI.Repository
{
    /// <summary>
    /// Repository for trust ratings.
    /// </summary>
    public class TrustRatingRepository : ITrustRatingRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly IReputationService _reputationService;

        /// <summary>
        /// Constructs a new TrustRatingRepository.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="reputationService">The reputation service.</param>
        public TrustRatingRepository(ApplicationDBContext context, IReputationService reputationService)
        {
            _context = context;
            _reputationService = reputationService;
        }

        /// <inheritdoc />
        public async Task<List<TrustRating>> GetAllAsync(GetTrustRatingDto getTrustRatingDto, string trustorId)
        {
            IQueryable<TrustRating> trustRatings = _context.TrustRatings.Include(tr => tr.Post);

            if (!string.IsNullOrEmpty(trustorId))
            {
                trustRatings = trustRatings.Where(tr => tr.TrustorId == trustorId);
            }

            if (!string.IsNullOrEmpty(getTrustRatingDto.TrusteeId))
            {
                trustRatings = trustRatings.Where(tr => tr.TrusteeId == getTrustRatingDto.TrusteeId);
            }

            if (getTrustRatingDto.PostId.HasValue)
            {
                trustRatings = trustRatings.Where(tr => tr.PostId == getTrustRatingDto.PostId.Value);
            }

            if (getTrustRatingDto.Page.HasValue && getTrustRatingDto.PageSize.HasValue)
            {
                int skip = (getTrustRatingDto.Page.Value - 1) * getTrustRatingDto.PageSize.Value;
                trustRatings = trustRatings.Skip(skip).Take(getTrustRatingDto.PageSize.Value);
            }

            return await trustRatings.ToListAsync();
        }

        /// <inheritdoc />
        public async Task<TrustRating?> GetByIdAsync(int id)
        {
            return await _context.TrustRatings.FindAsync(id);
        }

        /// <inheritdoc />
        public async Task<TrustRating> CreateAsync(TrustRating trustRating)
        {
            await _context.TrustRatings.AddAsync(trustRating);
            await _context.SaveChangesAsync();

            await _reputationService.UpdateAsync();

            return trustRating;
        }

        /// <inheritdoc />
        public async Task<TrustRating?> UpdateAsync(int id, UpdateTrustRatingDto updateTrustRatingDto)
        {
            TrustRating? trustRating = await _context.TrustRatings.FindAsync(id);

            if (trustRating == null)
            {
                return null;
            }

            trustRating.TrustValue = updateTrustRatingDto.TrustValue ?? trustRating.TrustValue;
            trustRating.Comment = updateTrustRatingDto.Comment ?? trustRating.Comment;

            await _context.SaveChangesAsync();

            await _reputationService.UpdateAsync();

            return trustRating;
        }

        /// <inheritdoc />
        public async Task<TrustRating?> DeleteAsync(int id)
        {
            TrustRating? trustRating = await _context.TrustRatings.FindAsync(id);

            if (trustRating == null)
            {
                return null;
            }

            _context.TrustRatings.Remove(trustRating);
            await _context.SaveChangesAsync();

            await _reputationService.UpdateAsync();

            return trustRating;
        }

        /// <inheritdoc />
        public async Task<bool> UserOwnsTrustRatingAsync(int trustRatingId, string userId)
        {
            TrustRating? trustRating = await _context.TrustRatings.FindAsync(trustRatingId);

            if (trustRating == null)
            {
                return false;
            }

            return trustRating.TrustorId == userId;
        }
    }
}