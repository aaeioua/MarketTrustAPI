using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace MarketTrustAPI.Dtos.User
{
    /// <summary>
    /// Represents the data required to update a user.
    /// </summary>
    public class UpdateUserDto
    {
        /// <summary>
        /// The new name of the user.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The new email of the user.
        /// </summary>
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// The new email visibility status of the user.
        /// </summary>
        public bool? IsPublicEmail { get; set; }
        
        /// <summary>
        /// The new phone number of the user.
        /// </summary>
        [Phone]
        public string? Phone { get; set; }

        /// <summary>
        /// The new phone visibility status of the user.
        /// </summary>
        public bool? IsPublicPhone { get; set; }

        /// <summary>
        /// The new longitude of the user's location.
        /// </summary>
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        /// <summary>
        /// The new latitude of the user's location.
        /// </summary>
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        /// <summary>
        /// The new location visibility status of the user.
        /// </summary>
        public bool? IsPublicLocation { get; set; }
    }
}