using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniNest.Domain;

namespace UniNest.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UniNestDbContext>();
        try
        {
            await db.Database.MigrateAsync();
        }
        catch
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
            }
            catch
            {
                // Proceed safely
            }
        }
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        foreach (var roleName in new[] { "Student", "Owner", "Admin" })
        {
            if (!await roles.RoleExistsAsync(roleName))
                await roles.CreateAsync(new AppRole { Name = roleName });
        }

        var locations = await SeedLocationsAsync(db);
        var amenities = await SeedAmenitiesAsync(db);
        await SeedUniversitiesAsync(db, locations);
        await SeedDemoUsersAndListingsAsync(db, users, locations, amenities);
    }

    private static async Task<Dictionary<string, Location>> SeedLocationsAsync(UniNestDbContext db)
    {
        var wanted = new (string En, string Ar)[]
        {
            ("Fayoum", "الفيوم"),
            ("Cairo", "القاهرة"),
            ("Giza", "الجيزة"),
            ("Alexandria", "الإسكندرية"),
            ("Mansoura", "المنصورة"),
            ("Zagazig", "الزقازيق")
        };

        foreach (var (en, ar) in wanted)
        {
            var exists = await db.Locations.AnyAsync(l => l.LocationType == LocationType.Governorate && l.NameEn == en);
            if (exists) continue;

            db.Locations.Add(new Location
            {
                Id = Guid.NewGuid(),
                LocationType = LocationType.Governorate,
                NameEn = en,
                NameAr = ar,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
        return await db.Locations.Where(l => l.LocationType == LocationType.Governorate)
            .ToDictionaryAsync(l => l.NameEn);
    }

    private static async Task<Dictionary<string, Amenity>> SeedAmenitiesAsync(UniNestDbContext db)
    {
        var wanted = new (string Code, string En, string Ar, short Order)[]
        {
            ("WIFI", "Wi-Fi", "واي فاي", 1),
            ("AC", "Air Conditioning", "تكييف", 2),
            ("KITCHEN", "Kitchen", "مطبخ", 3),
            ("WASHER", "Washing Machine", "غسالة", 4),
            ("PARKING", "Parking", "موقف سيارات", 5),
            ("ELEVATOR", "Elevator", "مصعد", 6)
        };

        foreach (var (code, en, ar, order) in wanted)
        {
            if (await db.Amenities.AnyAsync(a => a.Code == code)) continue;
            db.Amenities.Add(new Amenity
            {
                Id = Guid.NewGuid(),
                Code = code,
                NameEn = en,
                NameAr = ar,
                IsActive = true,
                SortOrder = order
            });
        }

        await db.SaveChangesAsync();
        return await db.Amenities.ToDictionaryAsync(a => a.Code);
    }

    private static async Task SeedUniversitiesAsync(UniNestDbContext db, IReadOnlyDictionary<string, Location> locations)
    {
        var wanted = new (string En, string Ar, string Code, string City)[]
        {
            ("Fayoum University", "جامعة الفيوم", "FAY", "Fayoum"),
            ("EELU Fayoum", "الجامعة المصرية للتعلم الإلكتروني بالفيوم", "EELU", "Fayoum"),
            ("Cairo University", "جامعة القاهرة", "CU", "Giza"),
            ("Ain Shams University", "جامعة عين شمس", "ASU", "Cairo"),
            ("Alexandria University", "جامعة الإسكندرية", "AU", "Alexandria"),
            ("Mansoura University", "جامعة المنصورة", "MU", "Mansoura"),
            ("Zagazig University", "جامعة الزقازيق", "ZU", "Zagazig")
        };

        foreach (var (en, ar, code, city) in wanted)
        {
            if (await db.Universities.AnyAsync(u => u.ShortCode == code)) continue;
            if (!locations.TryGetValue(city, out var location)) continue;

            db.Universities.Add(new University
            {
                Id = Guid.NewGuid(),
                LocationId = location.Id,
                NameEn = en,
                NameAr = ar,
                ShortCode = code,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoUsersAndListingsAsync(
        UniNestDbContext db,
        UserManager<AppUser> users,
        IReadOnlyDictionary<string, Location> locations,
        IReadOnlyDictionary<string, Amenity> amenities)
    {
        var owner = await EnsureUserAsync(users, "demo.owner@uninest.local", "Demo Owner", "Owner", Gender.Male, "DemoOwner!2345");
        await EnsureUserAsync(users, "demo.admin@uninest.local", "Demo Admin", "Admin", Gender.Unspecified, "DemoAdmin!2345");
        await EnsureUserAsync(users, "demo.student@uninest.local", "Demo Student", "Student", Gender.Male, "DemoStudent!2345");

        var now = DateTimeOffset.UtcNow;
        var seedListings = new[]
        {
            new { TitleEn = "Luxury 3-Bedroom Student Apartment", TitleAr = "شقة فاخرة 3 غرف نوم للطلاب", City = "Fayoum", Type = ListingType.EntireApartment, Policy = GenderPolicy.Any, Rent = 4500m, Rooms = (short)3, Beds = (short)3, Phone = "01007272508", Amenities = new[] { "WIFI", "AC", "KITCHEN", "ELEVATOR" }, DescEn = "Spacious 3-bedroom apartment near Fayoum University, fully furnished with high-speed internet, air conditioning, and a modern kitchen.", DescAr = "شقة واسعة 3 غرف نوم بالقرب من جامعة الفيوم، مفروشة بالكامل مع إنترنت عالي السرعة وتكييف ومطبخ حديث.", ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop&q=80" },
            new { TitleEn = "Modern Studio near EELU Fayoum", TitleAr = "استوديو حديث بالقرب من الجامعة الأهلية بالفيوم", City = "Fayoum", Type = ListingType.EntireApartment, Policy = GenderPolicy.MaleOnly, Rent = 3800m, Rooms = (short)1, Beds = (short)1, Phone = "01012557656", Amenities = new[] { "WIFI", "KITCHEN", "AC" }, DescEn = "Cozy single studio room, 10 minutes from EELU Fayoum branch, all utilities included with quiet study environment.", DescAr = "استوديو مفرد مريح، على بعد 10 دقائق من فرع الجامعة الأهلية بالفيوم، شامل كافة المرافق مع بيئة هادئة للدراسة.", ImageUrl = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800&auto=format&fit=crop&q=80" },
            new { TitleEn = "Spacious 4-Room Student Residence in Giza", TitleAr = "سكن طلاب 4 غرف واسع بالجيزة", City = "Giza", Type = ListingType.EntireApartment, Policy = GenderPolicy.Any, Rent = 7500m, Rooms = (short)4, Beds = (short)4, Phone = "01066805363", Amenities = new[] { "WIFI", "AC", "KITCHEN", "PARKING", "ELEVATOR" }, DescEn = "Prime location 4-room apartment 5 minutes from Cairo University. Features balcony, elevator, and fully equipped kitchen.", DescAr = "شقة 4 غرف بموقع متميز على بعد 5 دقائق من جامعة القاهرة. تتميز ببلكونة ومصعد ومطبخ مجهز بالكامل.", ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800&auto=format&fit=crop&q=80" },
            new { TitleEn = "Private Single Room near Alex University", TitleAr = "غرفة مفردة خاصة بالقرب من جامعة الإسكندرية", City = "Alexandria", Type = ListingType.PrivateRoom, Policy = GenderPolicy.Any, Rent = 3600m, Rooms = (short)1, Beds = (short)1, Phone = "01032894477", Amenities = new[] { "WIFI", "KITCHEN", "WASHER" }, DescEn = "Clean and quiet single room near Alexandria University campus with sea view and high speed Wi-Fi.", DescAr = "غرفة مفردة نظيفة وهادئة بالقرب من حرم جامعة الإسكندرية مع إطلالة رائعة وواي فاي سريع.", ImageUrl = "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?w=800&auto=format&fit=crop&q=80" },
            new { TitleEn = "Economical Shared Bed in Mansoura", TitleAr = "سرير في غرفة مشتركة بالمنصورة", City = "Mansoura", Type = ListingType.SharedBed, Policy = GenderPolicy.MaleOnly, Rent = 2200m, Rooms = (short)2, Beds = (short)4, Phone = "01009229692", Amenities = new[] { "WIFI", "WASHER" }, DescEn = "Affordable shared room for male students, close to Mansoura University and main transportation hubs.", DescAr = "سرير متاح في غرفة مشتركة بأسعار اقتصادية للطلاب، قريبة من جامعة المنصورة والمواصلات.", ImageUrl = "https://images.unsplash.com/photo-1598928506311-c55ded91a20c?w=800&auto=format&fit=crop&q=80" },
            new { TitleEn = "Premium 2-Bed Female Suite in Fayoum", TitleAr = "سويت مفروش للبنات 2 غرفة بالفيوم", City = "Fayoum", Type = ListingType.EntireApartment, Policy = GenderPolicy.FemaleOnly, Rent = 4200m, Rooms = (short)2, Beds = (short)2, Phone = "01166778899", Amenities = new[] { "WIFI", "AC", "KITCHEN", "WASHER" }, DescEn = "Exclusive cozy suite for female students with private security, balconies, and study desks.", DescAr = "سويت مريح مخصص للطالبات يوفر أمان خاص، بلكونات، ومكاتب للمذاكرة.", ImageUrl = "https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=800&auto=format&fit=crop&q=80" },
            new { TitleEn = "Modern 5-Bedroom Villa Flat near Ain Shams", TitleAr = "شقة 5 غرف واسعة بالقرب من عين شمس", City = "Cairo", Type = ListingType.EntireApartment, Policy = GenderPolicy.Any, Rent = 8500m, Rooms = (short)5, Beds = (short)5, Phone = "01011223344", Amenities = new[] { "WIFI", "AC", "KITCHEN", "PARKING", "ELEVATOR", "WASHER" }, DescEn = "Huge 5-bedroom luxury residence ideal for group student living near Ain Shams University.", DescAr = "شقة كبيرة 5 غرف نوم فاخرة مثالية لمجموعات الطلاب بالقرب من جامعة عين شمس.", ImageUrl = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=800&auto=format&fit=crop&q=80" },
            new { TitleEn = "Sunny Private Room in Alexandria", TitleAr = "غرفة خاصة مشمسة بالإسكندرية", City = "Alexandria", Type = ListingType.PrivateRoom, Policy = GenderPolicy.FemaleOnly, Rent = 3400m, Rooms = (short)1, Beds = (short)1, Phone = "01055443322", Amenities = new[] { "WIFI", "KITCHEN", "ELEVATOR" }, DescEn = "Bright private room for female student with desk, wardrobe, and high-speed Wi-Fi.", DescAr = "غرفة خاصة مشمسة ومضيئة لطالبة تحتوي على مكتب ودولاب وواي فاي سريع.", ImageUrl = "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=800&auto=format&fit=crop&q=80" }
        };

        foreach (var item in seedListings)
        {
            if (!locations.TryGetValue(item.City, out var location)) continue;

            var existingListing = await db.Listings.FirstOrDefaultAsync(l => l.TitleEn == item.TitleEn || l.TitleAr == item.TitleAr);
            Listing listing;
            if (existingListing != null)
            {
                listing = existingListing;
            }
            else
            {
                listing = new Listing
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = owner.Id,
                    LocationId = location.Id,
                    TitleEn = item.TitleEn,
                    TitleAr = item.TitleAr,
                    DescriptionEn = item.DescEn,
                    DescriptionAr = item.DescAr,
                    ListingType = item.Type,
                    GenderPolicy = item.Policy,
                    MonthlyRent = item.Rent,
                    Currency = "EGP",
                    RoomCount = item.Rooms,
                    TotalBeds = item.Beds,
                    AvailableBeds = item.Beds,
                    Status = ListingStatus.Published,
                    ContactPhone = item.Phone,
                    PublishedAt = now,
                    ModeratedAt = now
                };
                db.Listings.Add(listing);
            }

            var hasImg = await db.ListingImages.AnyAsync(li => li.ListingId == listing.Id);
            if (!hasImg)
            {
                var media = new MediaAsset
                {
                    Id = Guid.NewGuid(),
                    UploadedByUserId = owner.Id,
                    StorageKey = item.ImageUrl,
                    OriginalFileName = "property.jpg",
                    ContentType = "image/jpeg",
                    ByteSize = 1024 * 500,
                    ChecksumSha256 = Guid.NewGuid().ToString("N"),
                    ScanStatus = DocumentScanStatus.Clean,
                    CreatedAt = now
                };
                db.MediaAssets.Add(media);

                db.ListingImages.Add(new ListingImage
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    MediaAssetId = media.Id,
                    SortOrder = 0,
                    Status = ImageStatus.Approved,
                    IsPrimary = true,
                    CreatedAt = now
                });
            }

            foreach (var code in item.Amenities)
            {
                if (!amenities.TryGetValue(code, out var amenity)) continue;
                var hasAmenity = await db.ListingAmenities.AnyAsync(la => la.ListingId == listing.Id && la.AmenityId == amenity.Id);
                if (!hasAmenity)
                {
                    db.ListingAmenities.Add(new ListingAmenity
                    {
                        ListingId = listing.Id,
                        AmenityId = amenity.Id,
                        CreatedAt = now
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task<AppUser> EnsureUserAsync(
        UserManager<AppUser> users,
        string email,
        string displayName,
        string role,
        Gender gender,
        string password)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            Gender = gender,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        await users.AddToRoleAsync(user, role);
        return user;
    }
}
