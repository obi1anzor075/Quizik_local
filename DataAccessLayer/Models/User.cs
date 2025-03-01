﻿using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models
{
    public class User:IdentityUser
    {
        public string? GoogleId { get; set; }
        public string? Email { get; set; }
        [StringLength(100)]
        [MaxLength(100)]
        [Required]
        public string? Name { get; set; }
        public DateTime? CreatedAt { get; set; }
        public byte[]? ProfilePicture { get; set; }
        public string? ProfilePictureFileName { get; set; }
        public string? TwoFactorSecretKey { get; set; }

    }
}