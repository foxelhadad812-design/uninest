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

        if (await db.Listings.AnyAsync()) return;

        var now = DateTimeOffset.UtcNow;
        var seedListings = new[]
        {
            new { TitleEn = "Apartment near Fayoum University", TitleAr = "شقة بالقرب من جامعة الفيوم", City = "Fayoum", Type = ListingType.EntireApartment, Policy = GenderPolicy.Any, Rent = 3500m, Rooms = (short)2, Beds = (short)2, Phone = "01007272508", Amenities = new[] { "WIFI", "AC", "KITCHEN" }, DescEn = "Spacious apartment near Fayoum University, fully furnished, quiet neighborhood.", DescAr = "شقة واسعة بالقرب من جامعة الفيوم، مفروشة بالكامل، في حي هادئ." },
            new { TitleEn = "Single Room near EELU Fayoum", TitleAr = "غرفة مفردة بالقرب من الجامعة الأهلية بالفيوم", City = "Fayoum", Type = ListingType.PrivateRoom, Policy = GenderPolicy.MaleOnly, Rent = 3800m, Rooms = (short)1, Beds = (short)1, Phone = "01012557656", Amenities = new[] { "WIFI", "KITCHEN" }, DescEn = "Cozy single room, 10 minutes from EELU Fayoum branch, all utilities included.", DescAr = "غرفة مفردة مريحة، على بعد 10 دقائق من فرع الجامعة الأهلية بالفيوم، شاملة كل المرافق." },
            new { TitleEn = "Studio Apartment near Cairo University", TitleAr = "استوديو سكن بالقرب من جامعة القاهرة", City = "Giza", Type = ListingType.EntireApartment, Policy = GenderPolicy.Any, Rent = 6500m, Rooms = (short)1, Beds = (short)1, Phone = "01066805363", Amenities = new[] { "WIFI", "AC", "KITCHEN", "PARKING" }, DescEn = "Modern studio apartment, fully furnished, 5 minutes from Cairo University.", DescAr = "شقة استوديو حديثة مفروشة بالكامل، على بعد 5 دقائق من جامعة القاهرة." },
            new { TitleEn = "Single Room near Alexandria University", TitleAr = "غرفة مفردة بالقرب من جامعة الإسكندرية", City = "Alexandria", Type = ListingType.PrivateRoom, Policy = GenderPolicy.Any, Rent = 3600m, Rooms = (short)1, Beds = (short)1, Phone = "01032894477", Amenities = new[] { "WIFI", "KITCHEN" }, DescEn = "Clean and quiet single room near Alexandria University campus.", DescAr = "غرفة مفردة نظيفة وهادئة بالقرب من حرم جامعة الإسكندرية." },
            new { TitleEn = "Shared Room near Mansoura University", TitleAr = "غرفة مشتركة بالقرب من جامعة المنصورة", City = "Mansoura", Type = ListingType.SharedBed, Policy = GenderPolicy.MaleOnly, Rent = 3100m, Rooms = (short)1, Beds = (short)2, Phone = "01009229692", Amenities = new[] { "WIFI", "WASHER" }, DescEn = "Affordable shared room for students, close to transportation and markets.", DescAr = "غرفة مشتركة بأسعار اقتصادية للطلاب، قريبة من المواصلات والأسواق." },
            new { TitleEn = "Cozy Shared Room in Fayoum", TitleAr = "غرفة مشتركة مريحة للبنات", City = "Fayoum", Type = ListingType.SharedBed, Policy = GenderPolicy.FemaleOnly, Rent = 3900m, Rooms = (short)1, Beds = (short)2, Phone = "01166778899", Amenities = new[] { "WIFI", "AC" }, DescEn = "Recently painted cozy room with private bathroom access.", DescAr = "غرفة مريحة مطلية حديثاً مع حمام خاص." }
        };

        foreach (var item in seedListings)
        {
            if (!locations.TryGetValue(item.City, out var location)) continue;

            var listing = new Listing
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
            foreach (var code in item.Amenities)
            {
                if (!amenities.TryGetValue(code, out var amenity)) continue;
                db.ListingAmenities.Add(new ListingAmenity
                {
                    ListingId = listing.Id,
                    AmenityId = amenity.Id,
                    CreatedAt = now
                });
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
