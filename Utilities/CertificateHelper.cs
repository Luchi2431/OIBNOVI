using System;
using System.Security.Cryptography.X509Certificates;

namespace Oibnovi.Utilities;

public static class CertificateHelper
{
    /// <summary>
    /// Load an X.509 certificate from a file.
    /// </summary>
    public static X509Certificate2? LoadCertificate(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            return new X509Certificate2(filePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extract the CN (common name) from certificate subject.
    /// </summary>
    public static string? GetSubjectCN(X509Certificate2 cert)
    {
        var subject = cert.Subject;
        var parts = subject.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("CN="))
                return trimmed.Substring(3);
        }
        return null;
    }

    /// <summary>
    /// Extract the OU (organizational unit) from certificate subject.
    /// </summary>
    public static string? GetSubjectOU(X509Certificate2 cert)
    {
        var subject = cert.Subject;
        var parts = subject.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("OU="))
                return trimmed.Substring(3);
        }
        return null;
    }

    /// <summary>
    /// Get the issuer CN from certificate.
    /// </summary>
    public static string? GetIssuerCN(X509Certificate2 cert)
    {
        var issuer = cert.Issuer;
        var parts = issuer.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("CN="))
                return trimmed.Substring(3);
        }
        return null;
    }

    /// <summary>
    /// Validate that a user cert was issued by the service CA cert.
    /// </summary>
    public static bool ValidateIssuer(X509Certificate2 userCert, X509Certificate2 caCert)
    {
        // Simple check: issuer CN must match CA subject CN
        var userIssuerCN = GetIssuerCN(userCert);
        var caCN = GetSubjectCN(caCert);
        return userIssuerCN == caCN;
    }

    /// <summary>
    /// Check if certificate is within validity period.
    /// </summary>
    public static bool IsValid(X509Certificate2 cert)
    {
        var now = DateTime.UtcNow;

        // X509Certificate2 returns NotBefore/NotAfter in local system time with timezone info
        // Convert to UTC for proper comparison
        var notBeforeUTC = cert.NotBefore.ToUniversalTime();
        var notAfterUTC = cert.NotAfter.ToUniversalTime();

        var valid = now >= notBeforeUTC && now <= notAfterUTC;

        // Log for debugging
        File.AppendAllText("cert_debug.log", $"Cert valid: NotBefore={notBeforeUTC:O} NotAfter={notAfterUTC:O} Now={now:O} Result={valid}" + Environment.NewLine);

        return valid;
    }
}
