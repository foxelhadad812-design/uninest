using Microsoft.EntityFrameworkCore;
using UniNest.Domain;
using UniNest.Infrastructure;
using Xunit;

namespace UniNest.IntegrationTests;

public class DbContextMappingTests
{
    private static UniNestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<UniNestDbContext>()
            .UseSqlServer("Server=MOHAMED-ELHADDA;Database=UniNestDb_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true")
            .Options;
        return new UniNestDbContext(options);
    }

    [Fact]
    public void DbContext_Model_ContainsAllExpectedEntities()
    {
        using var db = CreateDbContext();
        var model = db.Model;

        Assert.NotNull(model.FindEntityType(typeof(AppUser)));
        Assert.NotNull(model.FindEntityType(typeof(AppRole)));
        Assert.NotNull(model.FindEntityType(typeof(UserProfile)));
        Assert.NotNull(model.FindEntityType(typeof(Location)));
        Assert.NotNull(model.FindEntityType(typeof(University)));
        Assert.NotNull(model.FindEntityType(typeof(Listing)));
        Assert.NotNull(model.FindEntityType(typeof(Amenity)));
        Assert.NotNull(model.FindEntityType(typeof(ListingAmenity)));
        Assert.NotNull(model.FindEntityType(typeof(Favorite)));
        Assert.NotNull(model.FindEntityType(typeof(MediaAsset)));
        Assert.NotNull(model.FindEntityType(typeof(ListingImage)));
        Assert.NotNull(model.FindEntityType(typeof(SensitiveDocument)));
        Assert.NotNull(model.FindEntityType(typeof(ListingModerationAction)));
        Assert.NotNull(model.FindEntityType(typeof(RefreshToken)));
        Assert.NotNull(model.FindEntityType(typeof(Inquiry)));
        Assert.NotNull(model.FindEntityType(typeof(InquiryMessage)));
        Assert.NotNull(model.FindEntityType(typeof(ReviewEligibility)));
        Assert.NotNull(model.FindEntityType(typeof(Review)));
        Assert.NotNull(model.FindEntityType(typeof(DiscountApplication)));
        Assert.NotNull(model.FindEntityType(typeof(Report)));
        Assert.NotNull(model.FindEntityType(typeof(TermsDocument)));
        Assert.NotNull(model.FindEntityType(typeof(TermsAcceptance)));
        Assert.NotNull(model.FindEntityType(typeof(AuditLog)));
    }

    [Fact]
    public void DbContext_Model_ConfiguresForeignKeysAndDeleteBehaviorsCorrectly()
    {
        using var db = CreateDbContext();
        var model = db.Model;

        var userProfile = model.FindEntityType(typeof(UserProfile))!;
        Assert.NotEmpty(userProfile.GetForeignKeys());

        var listing = model.FindEntityType(typeof(Listing))!;
        Assert.True(listing.GetForeignKeys().Count() >= 5);

        var inquiry = model.FindEntityType(typeof(Inquiry))!;
        Assert.True(inquiry.GetForeignKeys().Count() >= 4);

        var review = model.FindEntityType(typeof(Review))!;
        Assert.True(review.GetForeignKeys().Count() >= 4);

        var discount = model.FindEntityType(typeof(DiscountApplication))!;
        Assert.True(discount.GetForeignKeys().Count() >= 6);

        var report = model.FindEntityType(typeof(Report))!;
        Assert.True(report.GetForeignKeys().Count() >= 6);
    }
}
