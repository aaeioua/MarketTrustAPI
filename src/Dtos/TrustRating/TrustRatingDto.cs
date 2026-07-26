using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketTrustAPI.Dtos.TrustRating
{
    /// <summary>
    /// DTO for <see cref="TrustRating"/> model.
    /// </summary>
    public class TrustRatingDto
    {
        /// <summary>
        /// The ID of the trust rating.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the user who is giving the trust rating.
        /// </summary>
        public string TrustorId { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the user being rated.
        /// </summary>
        public string TrusteeId { get; set; } = string.Empty;

        /// <summary>
        /// The trust value given by the trustor to the trustee.
        /// </summary>
        public double TrustValue { get; set; }

        /// <summary>
        /// The time of creation of the trust rating.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The ID of the post associated with the trust rating, if any.
        /// </summary>
        public int? PostId { get; set; }

        /// <summary>
        /// The comment associated with this trust rating, if any.
        /// </summary>
        public string? Comment { get; set; }
    }
}