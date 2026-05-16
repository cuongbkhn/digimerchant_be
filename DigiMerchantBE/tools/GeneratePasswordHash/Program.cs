using Microsoft.AspNetCore.Identity;

var password = args.Length > 0 ? args[0] : "Admin@123";
var hasher = new PasswordHasher<SeedUser>();
var hash = hasher.HashPassword(new SeedUser(), password);
Console.WriteLine(hash);

file sealed class SeedUser;
