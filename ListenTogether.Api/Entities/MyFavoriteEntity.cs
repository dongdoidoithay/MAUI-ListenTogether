﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ListenTogether.Api.Entities
{
    [Table("MyFavorite")]
    public class MyFavoriteEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int UserBaseId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public DateTime EditTime { get; set; }
        public int MusicCount => Details.Count;
        public List<MyFavoriteDetailEntity> Details { get; set; } = null!;
    }
}
