using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MarketTrustAPI.Dtos.TrustRating
{
    /// <summary>
    /// Represents the data required to search for trust ratings.
    /// </summary>
    public class GetTrustRatingDto
    {
        /// <summary>
        /// The ID of the user who is giving the trust rating.
        /// </summary>
        public string? TrusteeId { get; set; }

        /// <summary>
        /// The page number for pagination.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
        public int? Page { get; set; }

        /// <summary>
        /// The number of items per page for pagination.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page size must be at least 1")]
        public int? PageSize { get; set; }
    }
}