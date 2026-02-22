using System;

namespace Oibnovi.Models;

/// <summary>
/// Represents a user with a certificate-based identity.
/// </summary>
public class User
{
    /// <summary>
    /// Username (must match certificate CN).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User group/role (must match certificate OU): "Službenik" or "Korisnik".
    /// </summary>
    public string Group { get; set; } = "Korisnik";

    /// <summary>
    /// Reference to the X.509 certificate file.
    /// </summary>
    public Certificate Cert { get; set; } = new Certificate();
}
