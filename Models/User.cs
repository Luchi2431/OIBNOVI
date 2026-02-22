using System;

namespace Oibnovi.Models;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string Group { get; set; } = "Korisnik"; // "Službenik" or "Korisnik"
    public Certificate Cert { get; set; } = new Certificate();
}
