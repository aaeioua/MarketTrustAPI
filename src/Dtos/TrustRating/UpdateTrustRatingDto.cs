using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MarketTrustAPI.Dtos.TrustRating
{
    /// <summary>
    /// Represents the data required to update a trust rating.
    /// </summary>
    public class UpdateTrustRatingDto
    {
        /// <summary>
        /// The new trust value.
        /// </summary>
        [Range(0, 5, ErrorMessage = "Trust value must be between 0 and 5.")]
        public double? TrustValue { get; set; }

        /// <summary>
        /// The new comment for the trust rating.
        /// </summary>
        public string? Comment { get; set; }
    }
}