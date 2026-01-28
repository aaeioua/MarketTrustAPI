using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.SpatialIndexManager;
using NetTopologySuite.Geometries;

namespace MarketTrustAPI.Models
{
    /// <summary>
    /// Represents a post.
    /// </summary>
    public class Post : ILocatable
    {
        /// <summary>
        /// The ID of the post.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The title of the post.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The content of the post.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// The time of creation.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The time of last update.
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// The ID of the user who created the post.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The user who created the post.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// The ID of the category to which the post belongs.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// The category to which the post belongs.
        /// </summary>
        public Category Category { get; set; } = null!;

        /// <summary>
        /// The <see cref="PropertyValue"/>s associated with the post.
        /// </summary>
        public List<PropertyValue> PropertyValues { get; set; } = new List<PropertyValue>();

        /// <summary>
        /// The price of the item in the post.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// The currency of the price in the post.
        /// </summary>
        public Currency? Currency { get; set; }

        /// <summary>
        /// Gets the geographical location of the author of the post.
        /// </summary>
        /// <returns>The author's location as a <see cref="Point"/>.</returns>
        public Point? GetLocation()
        {
            return User.GetLocation();
        }
    }

    /// <summary>
    /// Represents the currency of a post's price.
    /// </summary>
    public enum Currency
    {
        USD = 1
    }
}