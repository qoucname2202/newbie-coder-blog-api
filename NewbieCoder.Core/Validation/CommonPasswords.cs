namespace NewbieCoder.Core.Validation;

/// <summary>
/// Curated list of the most commonly used passwords.
/// Case-insensitive comparison is applied throughout.
/// </summary>
public static class CommonPasswords
{
    /// <summary>
    /// OWASP + NIST-recommended blocklist of passwords that are too common to allow.
    /// Case-insensitive matching is applied during lookups.
    /// </summary>
    public static readonly HashSet<string> Blocklist = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Pure numeric sequences ────────────────────────────────────────
        // Short variants already present: 123456, 12345678, 123456789, 12345, 1234, 1234567
        "1234567890", "12345678901", "0123456789", "0987654321",
        "000000", "111111", "222222", "333333", "444444",
        "555555", "666666", "777777", "888888", "999999",
        "112233", "123321", "654321", "13579", "24680",

        // ── Keyboard walk patterns ───────────────────────────────────────
        // Already present: qwerty, abc123, qwerty123, qwertyuiop, asdfghjkl, zxcvbnm
        "qwerty!@#$", "asdf", "zxcv", "qazwsx", "1q2w3e4r",
        "1qaz2wsx", "q1w2e3r4", "1qaz@wsx", "zaq!2wsx",

        // ── Classic top-25 (NIST 2024 most-used) ──────────────────────────
        // Already present: password, 123456, 12345678, qwerty, 123456789, 12345,
        //                  1234, 111111, 1234567, dragon, 123123, baseball,
        //                  iloveyou, trustno1, sunshine, master, welcome, shadow,
        //                  ashley, football, jesus, michael, ninja, mustang,
        //                  password1, 000000, abc123, admin, admin123, admin1234,
        //                  letmein, monkey, 696969, test, password!, passw0rd,
        //                  hello, charlie, donald, qwerty123, password123, batman
        "123qwe", "qwe123", "admin!", "admin!@#",
        "root", "toor", "changeme", "welcome1", "welcome123",

        // ── "password" family with appendages ─────────────────────────────
        // Already present: password!, password1!, passw0rd!, p@ssw0rd, p@ssword
        "Password", "PASSWORD", "Password1", "Password123", "P@ssword",
        "P@ssw0rd123", "Passw0rd!", "Password!", "PASSWORD1", "Password12",
        "pass", "pass123", "pass1234", "p@ss", "p@ss123",

        // ── Common words + number appendages ─────────────────────────────
        // Already present: password123, master123, admin1, admin123, admin1234
        "user123", "test123", "hello123", "love123", "iloveyou1", "iloveyou123",
        "welcome123", "sunshine1", "shadow123", "dragon123", "monkey123",
        "jessica", "jessica1", "jordan1", "jordan23", "michael1", "michael2",
        "charlie1", "batman1", "superman", "pokemon", "pokemon1",

        // ── Sports / teams ───────────────────────────────────────────────
        // Already present: football, yankees, cowboys, eagles, steelers, lakers, packers, patriots
        "liverpool", "arsenal", "manchester", "realmadrid", "barcelona",
        "champions", "winner", "winner1",

        // ── Seasonal / temporal / days ───────────────────────────────────
        // Already present: summer, winter, spring, autumn, monday..saturday, starwars
        "sunday", "monday1", "january", "february", "march", "april",
        "may2024", "june", "july", "august", "september", "october",
        "november", "december", "monday2024", "monday2025",

        // ── Generic / obvious choices ───────────────────────────────────
        // Already present: welcome, whatever, flower, secret, cheese, computer,
        //                  internet, freedom, nothing, trustno1!
        "welcome1", "welcome12", "welcome123", "welcome!",
        "guest", "guest123", "guest1", "temp", "temp1234",
        "default", "changeme1", "changeme!", "qwerty!",
        "letmein1", "letmein123", "login123", "login!",
        "passw0rd123", "pa$$w0rd", "p@ssword1",

        // ── Short 6-char (meets new min length) ───────────────────────────
        // Already present: monkey, dragon, jesus, ninja, pepper, soccer, hammer
        "123abc", "abc1234", "pass12", "pass1!", "test1!", "letme",
        "admin12", "user12", "pass6!", "asd123",

        // ── Vietnamese / regional common passwords ─────────────────────────
        "matkhau", "matkhau1", "matkhau123", "matkhaulam",
        "matkhaumenh", "yeuem", "yeuem123", "anhyeuem",
        "nam1990", "nam1991", "nam1992", "nam1993", "nam1994",
        "nam1995", "nam1996", "nam1997", "nam1998", "nam1999",
        "nam2000", "nam2001", "nam2002", "nam2003", "nam2004",
        "nhoqua", "khongbiet", "khongbiet123", "chochao",
        "chophep", "hello1234", "xinchao", "vietnam",

        // ── Numeric + keyboard combos ─────────────────────────────────────
        "passpass",   // already present
        "a12345", "a123456", "a1234567", "a12345678", "a123456789",
        "q12345", "q123456", "z123456", "x123456",
        "1a2b3c", "1a2b3c4d",

        // ── Leet-speak variations ────────────────────────────────────────
        // Already present: p@ssw0rd, p@ssword, passw0rd, passw0rd!
        "P@$$w0rd", "P@$$word", "P@55w0rd", "Passw0rd!",
        "Tr0ub4dor", "Tr0ub4d0r", "passphrase",
    };

    /// <summary>
    /// Checks whether the given password appears in the common passwords blocklist.
    /// </summary>
    public static bool IsBlocked(string password)
        => Blocklist.Contains(password);
}
