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
        public DbSet<tblRole> tblRole { get; set; }
        public DbSet<tblUser> tblUser { get; set; }
        public DbSet<tblRolePermission> tblRolePermission { get; set; }
        public DbSet<tblTable> tblTables { get; set; }
        public DbSet<tblTable_status> tblTable_status { get; set; }
        public DbSet<tblTable_type> tblTable_type { get; set; }
        public DbSet<tblArea> tblArea { get; set; }
        public DbSet<tblBooking_status> tblBooking_status { get; set; }
        public DbSet<tblOrder_cancelled> tblOrder_cancelled { get; set; }
        public DbSet<tblRestaurantInfo> tblRestaurantInfo { get; set; }
        public DbSet<tblContact> tblContact { get; set; }




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
                entity.Property(e => e.PriceCost).HasDefaultValue(0); // Thiết lập giá trị mặc định cho PriceCost
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

                entity.HasOne(d => d.Table)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.IdTable)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.Status)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.IdStatus)
                    .OnDelete(DeleteBehavior.SetNull);

            });

            modelBuilder.Entity<tblOrder>(entity =>
            {
                entity.HasKey(e => e.IdOrder); // 👈 Khai báo khóa chính

            });

            modelBuilder.Entity<tblOrder_cancelled>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("tblOrder_cancelled", table => table.ExcludeFromMigrations());
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CancelledTime)
                    .HasColumnName("CancellDate")
                    .HasColumnType("datetime");

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.Cancellations)
                    .HasForeignKey(e => e.IdOrder)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CancelledByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CancelledBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<tblOrder_detail>(entity =>
            {
                entity.HasKey(e => e.Id); // 👈 Khai báo khóa chính
        
            });
            
            modelBuilder.Entity<tblCustomer>(entity =>
            {
                entity.HasKey(e => e.IdCustomer);
                entity.HasOne(e => e.User)
                    .WithOne()
                    .HasForeignKey<tblCustomer>(e => e.IdUser)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<tblBranch>(entity =>
            {
                entity.HasKey(e => e.IdBranch); // 👈 Khai báo khóa chính
                
            });

            modelBuilder.Entity<tblRole>(entity =>
            {
                entity.HasKey(e => e.IdRole);
                entity.Property(e => e.IdRole).ValueGeneratedNever();
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            modelBuilder.Entity<tblRolePermission>(entity =>
            {
                entity.HasKey(e => new { e.IdRole, e.PermissionKey });
                entity.Property(e => e.PermissionKey).HasMaxLength(100);
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Permissions)
                    .HasForeignKey(e => e.IdRole)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<tblUser>(entity =>
            {
                entity.HasKey(e => e.IdUser);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.Property(e => e.Password).IsRequired().HasMaxLength(512);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.Avatar).HasMaxLength(255);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(e => e.IdRole)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<tblTable>(entity =>
            {
                entity.HasKey(e => e.IdTable);
                entity.Property(e => e.TableName).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);

                entity.HasOne(e => e.Area)
                    .WithMany(a => a.Tables)
                    .HasForeignKey(e => e.IdArea)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Status)
                    .WithMany(s => s.Tables)
                    .HasForeignKey(e => e.IdStatus)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Type)
                    .WithMany(t => t.Tables)
                    .HasForeignKey(e => e.IdType)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<tblTable_status>(entity =>
            {
                entity.HasKey(e => e.IdStatus);
                entity.Property(e => e.StatusName).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            modelBuilder.Entity<tblTable_type>(entity =>
            {
                entity.HasKey(e => e.IdType);
                entity.Property(e => e.TypeName).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            modelBuilder.Entity<tblBooking_status>(entity =>
            {
                entity.HasKey(e => e.IdStatus);
                entity.Property(e => e.StatusName).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
            });

            modelBuilder.Entity<tblArea>(entity =>
            {
                entity.HasKey(e => e.IdArea);
                entity.Property(e => e.AreaName).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(255);

                entity.HasOne(e => e.Branch)
                    .WithMany(b => b.Areas)
                    .HasForeignKey(e => e.IdBranch)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<tblContact>(entity =>
            {
                entity.HasKey(e => e.IdContact);
                entity.Property(e => e.Name).HasMaxLength(150);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Message).HasMaxLength(1000);
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");
                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<tblRestaurantInfo>(entity =>
            {
                entity.ToTable("tblRestaurant_info", table => table.ExcludeFromMigrations());
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ResName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Hotline1).HasMaxLength(15).IsUnicode(false);
                entity.Property(e => e.Hotline2).HasMaxLength(15).IsUnicode(false);
                entity.Property(e => e.Email).HasColumnName("Emai").HasMaxLength(255).IsUnicode(false);
                entity.Property(e => e.Logo).HasMaxLength(255).IsUnicode(false);
                entity.Property(e => e.OpeningDay).HasMaxLength(50);
                entity.Property(e => e.OpenTime).HasColumnType("time");
                entity.Property(e => e.CloseTime).HasColumnType("time");
                entity.Property(e => e.SortDescription).HasMaxLength(255);
                entity.Property(e => e.LogDescription).HasColumnType("ntext");
            });


        }
        
    }
}
