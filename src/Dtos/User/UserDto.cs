using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketTrustAPI.Dtos.Post;
using NetTopologySuite.Geometries;

namespace MarketTrustAPI.Dtos.User
{
    /// <summary>
    /// DTO for <see cref="User"/> model.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// The ID of the user.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the user.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The email of the user.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Indicates whether the user's email is publicly visible.
        /// </summary>
        public bool IsPublicEmail { get; set; }

        /// <summary>
        /// The phone number of the user.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Indicates whether the user's phone number is publicly visible.
        /// </summary>
        public bool IsPublicPhone { get; set; }

        /// <summary>
        /// The geographical location of the user.
        /// </summary>
        public Point? Location { get; set; }

        /// <summary>
        /// Indicates whether the user's location is publicly visible.
        /// </summary>
        public bool IsPublicLocation { get; set; }

        /// <summary>
        /// Indicates whether the user is pre-trusted (known to be trusted).
        /// </summary>
        public bool IsPretrusted { get; set; } = false;

        /// <summary>
        /// The posts created by the user.
        /// </summary>
        public List<PostDto> Posts { get; set; } = new List<PostDto>();
    }
}