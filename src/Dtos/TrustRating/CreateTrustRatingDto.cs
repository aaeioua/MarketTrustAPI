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
        [Range(0, 5, ErrorMessage = "Trust value must be between 0 and 5.")]
        public double TrustValue { get; set; }

        /// <summary>
        /// The ID of the post associated with the trust rating, if any.
        /// </summary>
        public int? PostId { get; set; }

        /// <summary>
        /// An optional comment about the trust rating.
        /// </summary>
        public string? Comment { get; set; }
    }
}