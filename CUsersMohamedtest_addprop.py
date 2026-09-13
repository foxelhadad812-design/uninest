import sys, json, subprocess

# Login as Owner
login = subprocess.run(
    ["curl", "-s", "-X", "POST", "http://localhost:5157/api/v1/auth/login",
     "-H", "Content-Type: application/json",
     "-d", '{"email":"demo.owner@uninest.local","password":"DemoOwner!2345"}'],
    capture_output=True, text=True, timeout=30
)
try:
    auth = json.loads(login.stdout)
    token = auth["accessToken"]
    print(f"LOGIN OK: token={token[:60]}...")
except Exception as e:
    print(f"LOGIN FAIL: {e}")
    print(f"Raw: {login.stdout[:200]}")
    sys.exit(1)

# Test /api/v1/auth/me
me = subprocess.run(
    ["curl", "-s", "http://localhost:5157/api/v1/auth/me",
     "-H", f"Authorization: Bearer {token}"],
    capture_output=True, text=True, timeout=15
)
try:
    me_data = json.loads(me.stdout)
    print(f"\nME OK: {me_data['displayName']} ({me_data['email']})")
    print(f"  Roles: {me_data.get('roles', [])}")
    print(f"  Gender: {me_data.get('gender', 'N/A')}")
except Exception as e:
    print(f"ME FAIL: {e}")

# Test /api/v1/listings/mine
mine = subprocess.run(
    ["curl", "-s", "http://localhost:5157/api/v1/listings/mine",
     "-H", f"Authorization: Bearer {token}"],
    capture_output=True, text=True, timeout=15
)
try:
    listings = json.loads(mine.stdout)
    print(f"\nMINE OK: {len(listings)} listings")
    for l in listings:
        print(f"  - {l['titleEn']} (status={l['status']}, price={l['monthlyRent']}, type={l['listingType']})")
except Exception as e:
    print(f"MINE FAIL: {e}")
    print(f"Raw: {mine.stdout[:200]}")

# Test create listing
create_payload = {
    "titleEn": "Test Listing from E2E",
    "titleAr": "قائمة اختبار",
    "descriptionEn": "Test description",
    "descriptionAr": "وصف اختبار",
    "listingType": "entireApartment",
    "genderPolicy": "Any",
    "monthlyRent": 5000,
    "roomCount": 2,
    "totalBeds": 2,
    "availableBeds": 2,
    "locationId": "0d06fc2e-f7bf-49d8-baea-5ca05c8fc171",
    "contactPhone": "01234567890",
    "amenityIds": []
}
create = subprocess.run(
    ["curl", "-s", "-X", "POST", "http://localhost:5157/api/v1/listings",
     "-H", "Content-Type: application/json",
     "-H", f"Authorization: Bearer {token}",
     "-d", json.dumps(create_payload)],
    capture_output=True, text=True, timeout=15
)
try:
    created = json.loads(create.stdout)
    print(f"\nCREATE OK: id={created['id']}")
    print(f"  Status: {created['status']}")
    print(f"  Title: {created['titleEn']}")
    print(f"  Price: {created['monthlyRent']}")
    
    # Verify it appears in /mine
    mine2 = subprocess.run(
        ["curl", "-s", "http://localhost:5157/api/v1/listings/mine",
         "-H", f"Authorization: Bearer {token}"],
        capture_output=True, text=True, timeout=15
    )
    mine_listings = json.loads(mine2.stdout)
    mine_ids = [l['id'] for l in mine_listings]
    if created['id'] in mine_ids:
        print(f"  VERIFIED in /mine: YES")
    else:
        print(f"  VERIFIED in /mine: NO (MISMATCH!)")
except Exception as e:
    print(f"CREATE FAIL: {e}")
    print(f"Raw: {create.stdout[:300]}")

# Test validation - missing required field
bad_payload = {
    "titleAr": "Only Arabic title",
    "listingType": "entireApartment",
    "monthlyRent": 3000,
    "locationId": "0d06fc2e-f7bf-49d8-baea-5ca05c8fc171"
}
bad = subprocess.run(
    ["curl", "-s", "-w", "\n%{http_code}", "-X", "POST", "http://localhost:5157/api/v1/listings",
     "-H", "Content-Type: application/json",
     "-H", f"Authorization: Bearer {token}",
     "-d", json.dumps(bad_payload)],
    capture_output=True, text=True, timeout=15
)
try:
    parts = bad.stdout.rsplit('\n', 1)
    status = int(parts[-1])
    body = parts[0] if len(parts) > 1 else ""
    print(f"\nVALIDATION (missing titleEn): HTTP {status}")
    if status == 400:
        err = json.loads(body)
        print(f"  Error: {err.get('detail', err.get('title', str(err)))}")
    else:
        print(f"  Body: {body[:200]}")
except Exception as e:
    print(f"VALIDATION FAIL: {e}")

# Verify the created listing via GET by ID
get_listing = subprocess.run(
    ["curl", "-s", f"http://localhost:5157/api/v1/listings/{created['id']}"],
    capture_output=True, text=True, timeout=15
)
try:
    gl = json.loads(get_listing.stdout)
    print(f"\nGET by ID OK: {gl['titleEn']} (status={gl['status']})")
except Exception as e:
    print(f"GET by ID FAIL: {e}")

print("\n=== ALL TESTS COMPLETE ===")
