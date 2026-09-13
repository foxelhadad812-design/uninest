using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using UniNest.Domain;

namespace UniNest.Infrastructure;

public sealed class UniNestDbContext(DbContextOptions<UniNestDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<ListingAmenity> ListingAmenities => Set<ListingAmenity>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<SensitiveDocument> SensitiveDocuments => Set<SensitiveDocument>();
    public DbSet<ListingModerationAction> ListingModerationActions => Set<ListingModerationAction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<InquiryMessage> InquiryMessages => Set<InquiryMessage>();
    public DbSet<ReviewEligibility> ReviewEligibilities => Set<ReviewEligibility>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<DiscountApplication> DiscountApplications => Set<DiscountApplication>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<TermsDocument> TermsDocuments => Set<TermsDocument>();
    public DbSet<TermsAcceptance> TermsAcceptances => Set<TermsAcceptance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditTimestamps();
        return base.SaveChanges();
    }

    private void UpdateAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        // Stamp entities that inherit AuditableEntity
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        // AppUser does not inherit AuditableEntity (IdentityUser<Guid> constraint),
        // so stamp it separately to avoid CreatedAt/UpdatedAt defaulting to 0001-01-01.
        foreach (var entry in ChangeTracker.Entries<AppUser>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        // b.HasPostgresExtension("pgcrypto");
        ConfigureIdentity(b);
        ConfigureReferenceData(b);
        ConfigureListings(b);
        ConfigureWorkflows(b);
    }

    private static void ConfigureIdentity(ModelBuilder b)
    {
        b.Entity<AppUser>(e =>
        {
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            e.Property(x => x.Gender).HasConversion<short>();
            // C-6: SQL defaults ensure CreatedAt/UpdatedAt are never 0001-01-01
            // even for raw SQL inserts or Identity-internal operations that
            // bypass the EF SaveChanges interceptor in UpdateAuditTimestamps().
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.DeletedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_AspNetUsers_Deleted", "[DeletedAt] IS NULL OR [IsActive] = 0"));
        });

        b.Entity<UserProfile>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.StudyProgram).HasMaxLength(150);
            e.Property(x => x.Bio).HasMaxLength(1000);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            e.HasOne<AppUser>().WithOne().HasForeignKey<UserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<University>().WithMany().HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.AvatarMediaId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.TokenHash).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.ExpiresAt }).HasFilter("[RevokedAt] IS NULL");
            e.HasIndex(x => new { x.TokenFamilyId, x.IssuedAt });
            e.ToTable(t => t.HasCheckConstraint("CK_RefreshTokens_Expiry", "[ExpiresAt] > [IssuedAt]"));

            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RefreshToken>().WithMany().HasForeignKey(x => x.ParentTokenId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<RefreshToken>().WithMany().HasForeignKey(x => x.ReplacedByTokenId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureReferenceData(ModelBuilder b)
    {
        b.Entity<Location>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.LocationType).HasConversion<short>();
            e.Property(x => x.ParentLocationType).HasConversion<short>();
            e.Property(x => x.NameEn).HasMaxLength(150).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.HasAlternateKey(x => new { x.Id, x.LocationType });
            e.HasIndex(x => new { x.ParentLocationId, x.LocationType, x.NameEn }).IsUnique();
            e.HasIndex(x => new { x.ParentLocationId, x.LocationType, x.NameAr }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Locations_Hierarchy", "([LocationType] = 1 AND [ParentLocationId] IS NULL AND [ParentLocationType] IS NULL) OR ([LocationType] = 2 AND [ParentLocationType] = 1) OR ([LocationType] = 3 AND [ParentLocationType] = 2) OR ([LocationType] = 4 AND [ParentLocationType] = 3)"));

            e.HasOne<Location>().WithMany().HasForeignKey(x => x.ParentLocationId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<University>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            e.Property(x => x.ShortCode).HasMaxLength(30);
            e.Property(x => x.Latitude).HasPrecision(9, 6);
            e.Property(x => x.Longitude).HasPrecision(9, 6);
            e.HasIndex(x => x.NameEn).IsUnique();
            e.HasIndex(x => x.NameAr).IsUnique();
            e.HasIndex(x => x.ShortCode).IsUnique().HasFilter("[ShortCode] IS NOT NULL");
            e.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Amenity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.NameEn).HasMaxLength(150).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.NameEn).IsUnique();
            e.HasIndex(x => x.NameAr).IsUnique();
        });
    }

    private static void ConfigureListings(ModelBuilder b)
    {
        b.Entity<Listing>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.TitleEn).HasMaxLength(200).IsRequired();
            e.Property(x => x.TitleAr).HasMaxLength(200);
            e.Property(x => x.DescriptionEn).HasMaxLength(4000).IsRequired();
            e.Property(x => x.DescriptionAr).HasMaxLength(4000);
            e.Property(x => x.MonthlyRent).HasPrecision(12, 2);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.ContactPhone).HasMaxLength(30);
            e.Property(x => x.ModerationReason).HasMaxLength(1000);
            e.Property(x => x.ListingType).HasConversion<short>();
            e.Property(x => x.GenderPolicy).HasConversion<short>();
            e.Property(x => x.Status).HasConversion<short>();
            e.HasIndex(x => new { x.Status, x.DeletedAt, x.PublishedAt });
            e.HasIndex(x => new { x.LocationId, x.Status, x.DeletedAt, x.MonthlyRent });
            e.HasIndex(x => new { x.OwnerUserId, x.Status, x.DeletedAt, x.UpdatedAt });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Listings_Rent", "[MonthlyRent] > 0");
                t.HasCheckConstraint("CK_Listings_Beds", "[TotalBeds] >= 1 AND [AvailableBeds] BETWEEN 0 AND [TotalBeds]");
            });

            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<University>().WithMany().HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ModeratedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.DeletedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ListingAmenity>(e =>
        {
            e.HasKey(x => new { x.ListingId, x.AmenityId });
            e.HasIndex(x => new { x.AmenityId, x.ListingId });
            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Amenity>().WithMany().HasForeignKey(x => x.AmenityId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Favorite>(e =>
        {
            e.HasKey(x => new { x.UserId, x.ListingId });
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MediaAsset>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            e.Property(x => x.OriginalFileName).HasMaxLength(256).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.ChecksumSha256).HasMaxLength(64).IsRequired();
            e.Property(x => x.ScanStatus).HasConversion<short>();
            e.HasIndex(x => x.StorageKey).IsUnique();
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ListingImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Status).HasConversion<short>();
            e.HasIndex(x => new { x.ListingId, x.SortOrder }).IsUnique().HasFilter("[DeletedAt] IS NULL");
            e.HasIndex(x => x.ListingId).IsUnique().HasFilter("[IsPrimary] = 1 AND [DeletedAt] IS NULL");
            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ModeratedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SensitiveDocument>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.DocumentType).HasConversion<short>();
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.UploadedMediaAssetId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ListingModerationAction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Action).HasConversion<short>();
            e.Property(x => x.FromStatus).HasConversion<short>();
            e.Property(x => x.ToStatus).HasConversion<short>();
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.Property(x => x.InternalNotes).HasMaxLength(2000);
            e.HasIndex(x => new { x.ListingId, x.CreatedAt });
            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureWorkflows(ModelBuilder b)
    {
        b.Entity<Inquiry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Status).HasConversion<short>();
            e.HasIndex(x => new { x.StudentUserId, x.ListingId }).IsUnique().HasFilter("[Status] IN (1, 2)");
            e.HasIndex(x => new { x.OwnerUserId, x.Status, x.UpdatedAt });
            e.ToTable(t => t.HasCheckConstraint("CK_Inquiries_DifferentUsers", "[StudentUserId] <> [OwnerUserId]"));

            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<InquiryMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.ModerationReason).HasMaxLength(1000);
            e.HasIndex(x => new { x.InquiryId, x.CreatedAt });
            e.HasOne<Inquiry>().WithMany().HasForeignKey(x => x.InquiryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ReviewEligibility>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Source).HasConversion<short>();
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasIndex(x => new { x.UserId, x.ListingId }).IsUnique().HasFilter("[RevokedAt] IS NULL");

            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Inquiry>().WithMany().HasForeignKey(x => x.SourceInquiryId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Review>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Status).HasConversion<short>();
            e.Property(x => x.Body).HasMaxLength(4000);
            e.HasIndex(x => new { x.AuthorUserId, x.ListingId }).IsUnique().HasFilter("[DeletedAt] IS NULL");
            e.HasIndex(x => x.ReviewEligibilityId).IsUnique();
            e.HasIndex(x => new { x.ListingId, x.Status, x.CreatedAt });
            e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));

            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ReviewEligibility>().WithMany().HasForeignKey(x => x.ReviewEligibilityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ModeratedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DiscountApplication>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Status).HasConversion<short>();
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.DecisionReason).HasMaxLength(1000);
            e.HasIndex(x => new { x.StudentUserId, x.ListingId }).IsUnique().HasFilter("[Status] IN (1, 2, 3)");
            e.HasIndex(x => new { x.Status, x.SubmittedAt });
            e.ToTable(t => t.HasCheckConstraint("CK_DiscountApplications_Percent", "[DiscountPercent] BETWEEN 0 AND 100"));

            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<University>().WithMany().HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SensitiveDocument>().WithMany().HasForeignKey(x => x.UniversityIdDocumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SensitiveDocument>().WithMany().HasForeignKey(x => x.NationalIdDocumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Report>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Status).HasConversion<short>();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.ResolutionNotes).HasMaxLength(2000);
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.ToTable(t => t.HasCheckConstraint("CK_Reports_OneTarget", "(CASE WHEN [ReportedListingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ReportedReviewId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ReportedUserId] IS NULL THEN 0 ELSE 1 END) = 1"));

            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReporterUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Listing>().WithMany().HasForeignKey(x => x.ReportedListingId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Review>().WithMany().HasForeignKey(x => x.ReportedReviewId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReportedUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AssignedAdminUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<TermsDocument>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.DocumentType).HasConversion<short>();
            e.Property(x => x.Version).HasMaxLength(50).IsRequired();
            e.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.ContentUrl).HasMaxLength(2048).IsRequired();
            e.HasIndex(x => new { x.DocumentType, x.Version }).IsUnique();
            e.HasIndex(x => new { x.DocumentType, x.ContentHash }).IsUnique();
        });

        b.Entity<TermsAcceptance>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.AcceptedRoleName).HasMaxLength(50).IsRequired();
            e.Property(x => x.IpHash).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.HasIndex(x => new { x.UserId, x.TermsDocumentId }).IsUnique().HasFilter("[DeletedAt] IS NULL");

            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<TermsDocument>().WithMany().HasForeignKey(x => x.TermsDocumentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.IpHash).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.OldValues).HasColumnType("nvarchar(max)");
            e.Property(x => x.NewValues).HasColumnType("nvarchar(max)");
            e.Property(x => x.Metadata).HasColumnType("nvarchar(max)");
            e.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
            e.HasIndex(x => new { x.ActorUserId, x.OccurredAt });

            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

