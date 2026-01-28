using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.Models;

namespace MarketTrustAPI.Dtos.Post
{
    /// <summary>
    /// Represents the data required to update an existing post.
    /// </summary>
    public class UpdatePostDto
    {
        /// <summary>
        /// The new title of the post.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The new content of the post.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// The new category ID to which the post belongs.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// The new price of the item in the post.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// The new currency of the price in the post.
        /// </summary>
        public Currency? Currency { get; set; }
    }
}