using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.Dtos.TrustRating;
using MarketTrustAPI.Models;

namespace MarketTrustAPI.Mappers
{
    public static class TrustRatingMapper
    {
        public static TrustRatingDto ToTrustRatingDto(this TrustRating trustRating)
        {
            return new TrustRatingDto
            {
                Id = trustRating.Id,
                TrustorId = trustRating.TrustorId,
                TrusteeId = trustRating.TrusteeId,
                TrustValue = trustRating.TrustValue,
                CreatedAt = trustRating.CreatedAt,
                PostId = trustRating.PostId,
                Comment = trustRating.Comment
            };
        }

        public static TrustRating ToTrustRatingFromCreateDto(this CreateTrustRatingDto createTrustRatingDto, string trustorId)
        {
            return new TrustRating
            {
                TrustorId = trustorId,
                TrusteeId = createTrustRatingDto.TrusteeId,
                TrustValue = createTrustRatingDto.TrustValue,
                CreatedAt = DateTime.Now,
                PostId = createTrustRatingDto.PostId,
                Comment = createTrustRatingDto.Comment
            };
        }
    }
}