using BCrypt.Net;

const string AdminPassword = "Admin@123456";
const string TestPassword  = "Test@123456";

string hash1 = BCrypt.Net.BCrypt.HashPassword(AdminPassword, 11);
string hash2 = BCrypt.Net.BCrypt.HashPassword(AdminPassword, 11);
string hash3 = BCrypt.Net.BCrypt.HashPassword(TestPassword, 11);

Console.WriteLine("=== BCrypt Hashes (work factor = 11) ===");
Console.WriteLine();
Console.WriteLine($"Admin1 password: {AdminPassword}");
Console.WriteLine($"Hash: {hash1}");
Console.WriteLine();
Console.WriteLine($"Admin2 password: {AdminPassword}");
Console.WriteLine($"Hash: {hash2}");
Console.WriteLine();
Console.WriteLine($"Test user password: {TestPassword}");
Console.WriteLine($"Hash: {hash3}");

// Verify
Console.WriteLine();
Console.WriteLine("=== Verification ===");
Console.WriteLine($"Admin1 verify: {BCrypt.Net.BCrypt.Verify(AdminPassword, hash1)}");
Console.WriteLine($"Admin2 verify: {BCrypt.Net.BCrypt.Verify(AdminPassword, hash2)}");
Console.WriteLine($"Test verify:   {BCrypt.Net.BCrypt.Verify(TestPassword, hash3)}");
