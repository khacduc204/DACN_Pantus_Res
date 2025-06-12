using Microsoft.EntityFrameworkCore;



namespace KD_Restaurant.Models
{
    public partial class KDContext : DbContext
    {
        public KDContext(DbContextOptions<KDContext> options) : base(options)
        {
        }

        public DbSet<tblMenu> tblMenu { get; set; }

        public DbSet<tblSlider> tblSlider { get; set; }

        public DbSet<tblMenuItem> tblMenuItem { get; set; }
        
        public DbSet<tblMenuCategory> tblMenuCategory { get; set; }

        public DbSet<tblMenuReview> tblMenuReview { get; set; }

        public DbSet<tblCustomer> tblCustomer { get; set; }
        public DbSet<tblBooking> tblBooking { get; set; }
        public DbSet<tblOrder> tblOrder { get; set; }
        public DbSet<tblOrder_detail> tblOrder_detail { get; set; }
        public DbSet<tblBranch> tblBranch { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<tblMenu>(entity =>
            {
                entity.HasKey(e => e.IdMenu); // 👈 Khai báo khóa chính
            });

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<tblSlider>().HasKey(s => s.IdSlider);

            modelBuilder.Entity<tblMenuItem>(entity =>
            {
                entity.HasKey(e => e.IdMenuItem); // 👈 Khai báo khóa chính
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()"); // Thiết lập giá trị mặc định cho CreatedDate
                entity.Property(e => e.ModifiedDate).HasDefaultValueSql("getdate()"); // Thiết lập giá trị mặc định cho ModifiedDate
                entity.HasOne(d => d.Category)
                    .WithMany(p => p.tblMenuItems)
                    .HasForeignKey(d => d.IdCategory)
                    .OnDelete(DeleteBehavior.Cascade); // Thiết lập quan hệ với tblMenuCategory
                entity.Property(e => e.IsActive).HasDefaultValue(true); // Thiết lập giá trị mặc định cho IsActive
                entity.Property(e => e.PriceSale).HasDefaultValue(0); // Thiết lập giá trị mặc định cho PriceSale
                entity.Property(e => e.Quantity).HasDefaultValue(0); // Thiết lập giá trị mặc định cho Quantity
                entity.Property(e => e.Star).HasDefaultValue(0); // Thiết lập giá trị mặc định cho Star
                entity.Property(e => e.Detail).HasDefaultValue(""); // Thiết lập giá trị mặc định cho Detail
                entity.Property(e => e.Image).HasDefaultValue(""); // Thiết lập giá trị mặc định cho Image

            });
            modelBuilder.Entity<tblMenuCategory>(entity =>
            {
                entity.HasKey(e => e.IdCategory); // 👈 Khai báo khóa chính
                entity.Property(e => e.IsActive).HasDefaultValue(true); // Thiết lập giá trị mặc định cho IsActive
                entity.Property(e => e.Image).HasDefaultValue(""); // Thiết lập giá trị mặc định cho Image
                entity.Property(e => e.Title).HasDefaultValue(""); // Thiết lập giá trị mặc định cho Title
                entity.Property(e => e.Alias).HasDefaultValue(""); // Thiết lập giá trị mặc định cho Alias
                entity.Property(e => e.Description).HasDefaultValue(""); // Thiết lập giá trị mặc định cho Description

            });

            modelBuilder.Entity<tblMenuReview>(entity =>
            {
                entity.HasKey(e => e.IdMenuReview); // 👈 Khai báo khóa chính
                entity.Property(e => e.CreatedBy).HasMaxLength(150);
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");
                entity.Property(e => e.Detail).HasMaxLength(500);
                entity.Property(e => e.Image).HasMaxLength(150);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Name).HasMaxLength(150);

                entity.HasOne(d => d.MenuItem).WithMany(p => p.tblMenuReview)
                .HasForeignKey(d => d.IdMenuItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tb_MenuRe__MenuI__76969D2E");
            });

            modelBuilder.Entity<tblSlider>(entity =>
            {
                entity.HasKey(e => e.IdSlider); // 👈 Khai báo khóa chính
                entity.Property(e => e.ImagePath).IsRequired().HasMaxLength(150); // Thiết lập ImagePath là bắt buộc
                entity.Property(e => e.Title).HasMaxLength(150); // Thiết lập độ dài tối đa cho Title
                entity.Property(e => e.Description).HasMaxLength(500); // Thiết lập độ dài tối đa cho Description
                entity.Property(e => e.IsActive).HasDefaultValue(true); // Thiết lập giá trị mặc định cho IsActive
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0); // Thiết lập giá trị mặc định cho DisplayOrder
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("getdate()"); // Thiết lập giá trị mặc định cho CreatedDate
            });

            modelBuilder.Entity<tblBooking>(entity =>
            {
                entity.HasKey(e => e.IdBooking);

                entity.HasOne(d => d.Branch)
                    .WithMany(p => p.tblBooking)
                    .HasForeignKey(d => d.IdBranch)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.tblBooking)
                    .HasForeignKey(d => d.IdCustomer)
                    .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<tblOrder>(entity =>
            {
                entity.HasKey(e => e.IdOrder); // 👈 Khai báo khóa chính

            });

            modelBuilder.Entity<tblOrder_detail>(entity =>
            {
                entity.HasKey(e => e.Id); // 👈 Khai báo khóa chính
        
            });
            
            modelBuilder.Entity<tblCustomer>(entity =>
            {
                entity.HasKey(e => e.IdCustomer); // 👈 Khai báo khóa chính
            
            });

            modelBuilder.Entity<tblBranch>(entity =>
            {
                entity.HasKey(e => e.IdBranch); // 👈 Khai báo khóa chính
                
            });


        }
        
    }
}
