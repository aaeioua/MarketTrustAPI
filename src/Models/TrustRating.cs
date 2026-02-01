using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketTrustAPI.Models
{
    /// <summary>
    /// Represents a trust rating between two users.
    /// </summary>
    public class TrustRating
    {
        /// <summary>
        /// The ID of the trust rating.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The ID of the user who gives the trust rating.
        /// </summary>
        public string TrustorId { get; set; } = string.Empty;

        /// <summary>
        /// The user who gives the trust rating.
        /// </summary>
        public User Trustor { get; set; } = null!;

        /// <summary>
        /// The ID of the user being rated.
        /// </summary>
        public string TrusteeId { get; set; } = string.Empty;

        /// <summary>
        /// The user being rated.
        /// </summary>
        public User Trustee { get; set; } = null!;

        /// <summary>
        /// The trust value given by the trustor to the trustee.
        /// Higher values indicate higher trust.
        /// </summary>
        public double TrustValue { get; set; }

        /// <summary>
        /// The time of creation.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// An optional comment for the trust rating.
        /// </summary>
        public string? Comment { get; set; }
    }
}