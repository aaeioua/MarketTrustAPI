using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.ReputationManager;
using Microsoft.AspNetCore.Mvc;

namespace MarketTrustAPI.Dtos.Post
{
    /// <summary>
    /// Represents the data for searching for posts.
    /// </summary>
    [BindProperties]
    public class GetPostDto
    {
        /// <summary>
        /// The title of the post to search for.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The contents (including properties) of the post to search for.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// The category ID to filter posts by.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// The longitude of the post's location for spatial search.
        /// </summary>
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        /// <summary>
        /// The latitude of the post's location for spatial search.
        /// </summary>
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        /// <summary>
        /// The search radius in meters for spatial search.
        /// </summary>
        public double? SearchRadius { get; set; }

        /// <summary>
        /// See <see cref="IReputationManager.GetPersonalTrust(int, double)"/> for details.
        /// </summary>
        [Range(0, 1, ErrorMessage = "d must be between 0 and 1")]
        public double? D { get; set; }
        
        /// <summary>
        /// Indicates whether to sort by price in ascending order. If false, sorts in descending order.
        /// </summary>
        public bool? IsPriceAscending { get; set; }
        
        /// <summary>
        /// Page number for pagination.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be atleast 1")]
        public int? Page { get; set; }

        /// <summary>
        /// Number of posts per page for pagination.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page size must be atleast 1")]
        public int? PageSize { get; set; }
    }
}