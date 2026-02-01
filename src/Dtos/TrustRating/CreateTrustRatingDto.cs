using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MarketTrustAPI.Dtos.TrustRating
{
    /// <summary>
    /// Represents the data required to create a trust rating.
    /// </summary>
    public class CreateTrustRatingDto
    {
        /// <summary>
        /// The ID of the user who is giving the trust rating.
        /// </summary>
        [Required]
        public string TrusteeId { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the user being rated.
        /// </summary>
        [Required]
        public double TrustValue { get; set; }

        /// <summary>
        /// An optional comment about the trust rating.
        /// </summary>
        public string? Comment { get; set; }
    }
}