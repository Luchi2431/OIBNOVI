using System;

namespace Oibnovi.Models;

public class Account
{
    public long Number { get; set; }
    public double Balance { get; set; }
    public double AllowedMinus { get; set; }
    public bool Blocked { get; set; }
    public DateTime LastTransaction { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
}
