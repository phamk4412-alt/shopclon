namespace shop.data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("SanPham")]
    public partial class SanPham
    {
        [Key]
        [StringLength(20)]
        public string MaSP { get; set; }

        [Required]
        [StringLength(100)]
        public string TenSP { get; set; }

        public decimal Gia { get; set; }

        [StringLength(255)]
        public string Hinh { get; set; }
    }
}
