using System.Security.Cryptography.X509Certificates;
using Oibnovi.Utilities;

namespace Oibnovi.Models;

public class Certificate
{
    /// <summary>
    /// Path to the X.509 certificate file (e.g., "certs/admin.crt").
    /// </summary>
    public string CertPath { get; set; } = string.Empty;

    /// <summary>
    /// Cached X509Certificate2 after loading.
    /// </summary>
    private X509Certificate2? _loaded;

    /// <summary>
    /// Load the certificate from disk and cache it.
    /// </summary>
    public bool Load()
    {
        _loaded = CertificateHelper.LoadCertificate(CertPath);
        return _loaded != null;
    }

    /// <summary>
    /// Get the loaded X509Certificate2 object (or null if not loaded).
    /// </summary>
    public X509Certificate2? GetX509Cert() => _loaded;

    /// <summary>
    /// Extract issuer CN from the loaded certificate.
    /// </summary>
    public string? GetIssuerCN() => _loaded != null ? CertificateHelper.GetIssuerCN(_loaded) : null;

    /// <summary>
    /// Extract subject CN from the loaded certificate.
    /// </summary>
    public string? GetSubjectCN() => _loaded != null ? CertificateHelper.GetSubjectCN(_loaded) : null;

    /// <summary>
    /// Extract subject OU (group) from the loaded certificate.
    /// </summary>
    public string? GetSubjectOU() => _loaded != null ? CertificateHelper.GetSubjectOU(_loaded) : null;

    /// <summary>
    /// Check if certificate is valid (not expired, not before).
    /// </summary>
    public bool IsValid() => _loaded != null && CertificateHelper.IsValid(_loaded);
}
