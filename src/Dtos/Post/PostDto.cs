using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.Dtos.PropertyValue;
using MarketTrustAPI.Models;

namespace MarketTrustAPI.Dtos.Post
{
    /// <summary>
    /// DTO for <see cref="Post"/> model.
    /// </summary>
    public class PostDto
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
        /// The global trust value of the author.
        /// </summary>
        public double? GlobalTrust { get; set; }

        /// <summary>
        /// The personal trust value given by the user to the post author.
        /// </summary>
        public double? PersonalTrust { get; set; }

        /// <summary>
        /// The creation time of the post.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The last update time of the post.
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// The ID of the user who created the post.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the category to which the post belongs.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// The properties of the post.
        /// </summary>
        public List<PropertyValueDto> PropertyValues { get; set; } = new List<PropertyValueDto>();

        /// <summary>
        /// The price of the item in the post.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// The currency of the price in the post.
        /// </summary>
        public Currency? Currency { get; set; }
    }
}