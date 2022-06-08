using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace API.Models
{
    public partial class PontajDbContext : DbContext
    {
        public PontajDbContext()
        {
        }

        public PontajDbContext(DbContextOptions<PontajDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Credential> Credentials { get; set; }
        public virtual DbSet<EverySingleDay> EverySingleDays { get; set; }
        public virtual DbSet<FisaPostuluiDeBaza> FisaPostuluiDeBazas { get; set; }
        public virtual DbSet<PlataCuOra> PlataCuOras { get; set; }
        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<ProjectsProposedTimeInterval> ProjectsProposedTimeIntervals { get; set; }
        public virtual DbSet<ProjectsTimeInterval> ProjectsTimeIntervals { get; set; }
        public virtual DbSet<ProjectsUser> ProjectsUsers { get; set; }
        public virtual DbSet<Token> Tokens { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=10.13.0.2;Database=PontajDB;User Id=SA;password=kP998#.op23J;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<Credential>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<EverySingleDay>(entity =>
            {
                entity.HasKey(e => e.DayId);

                entity.ToTable("EverySingleDay");

                entity.HasIndex(e => e.UserId, "IX_EverySingleDay_UserId");

                entity.Property(e => e.Date).HasColumnType("date");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.EverySingleDays)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_EverySingleDay_Credentials");
            });

            modelBuilder.Entity<FisaPostuluiDeBaza>(entity =>
            {
                entity.ToTable("FisaPostuluiDeBaza");

                entity.HasIndex(e => e.DayId, "IX_FisaPostuluiDeBaza_DayId");

                entity.HasOne(d => d.Day)
                    .WithMany(p => p.FisaPostuluiDeBazas)
                    .HasForeignKey(d => d.DayId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FisaPostuluiDeBaza_EverySingleDay");
            });

            modelBuilder.Entity<PlataCuOra>(entity =>
            {
                entity.ToTable("PlataCuOra");

                entity.HasIndex(e => e.DayId, "IX_PlataCuOra_DayId");

                entity.Property(e => e.AppForOnline).HasMaxLength(50);

                entity.Property(e => e.SubjectName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.Day)
                    .WithMany(p => p.PlataCuOras)
                    .HasForeignKey(d => d.DayId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PlataCuOra_EverySingleDay");
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasIndex(e => e.DirectorId, "IX_Projects_DirectorId");

                entity.Property(e => e.ProjectName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.Director)
                    .WithMany(p => p.Projects)
                    .HasForeignKey(d => d.DirectorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Projects_Credentials");
            });

            modelBuilder.Entity<ProjectsProposedTimeInterval>(entity =>
            {
                entity.HasIndex(e => e.DayId, "IX_ProjectsProposedTimeIntervals_DayId");

                entity.HasIndex(e => e.ProjectId, "IX_ProjectsProposedTimeIntervals_ProjectId");

                entity.HasOne(d => d.Day)
                    .WithMany(p => p.ProjectsProposedTimeIntervals)
                    .HasForeignKey(d => d.DayId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectsProposedTimeIntervals_EverySingleDay");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectsProposedTimeIntervals)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectsProposedTimeIntervals_Projects");
            });

            modelBuilder.Entity<ProjectsTimeInterval>(entity =>
            {
                entity.ToTable("ProjectsTimeInterval");

                entity.HasIndex(e => e.DayId, "IX_ProjectsTimeInterval_DayId");

                entity.HasOne(d => d.Day)
                    .WithMany(p => p.ProjectsTimeIntervals)
                    .HasForeignKey(d => d.DayId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ActionsTimeInterval_EverySingleDay");

                entity.HasOne(d => d.DayNavigation)
                    .WithMany(p => p.ProjectsTimeIntervals)
                    .HasForeignKey(d => d.DayId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectsTimeInterval_Projects");
            });

            modelBuilder.Entity<ProjectsUser>(entity =>
            {
                entity.ToTable("Projects_Users");

                entity.HasIndex(e => e.ProjectId, "IX_Projects_Users_ProjectId");

                entity.HasIndex(e => e.UserId, "IX_Projects_Users_UserId");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectsUsers)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Projects_Users_Projects");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ProjectsUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Projects_Users_Credentials");
            });

            modelBuilder.Entity<Token>(entity =>
            {
                entity.Property(e => e.Token1)
                    .IsRequired()
                    .HasMaxLength(256)
                    .IsUnicode(false)
                    .HasColumnName("Token")
                    .IsFixedLength(true);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Tokens)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Tokens_Credentials");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.UserId, "IX_Users_UserId");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Mail)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(d => d.UserNavigation)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Users_Credentials");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
