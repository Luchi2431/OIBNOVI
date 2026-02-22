using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Oibnovi.Utilities;
using Xunit;

namespace Oibnovi.Tests;

public class CertificateHelperTests
{
    [Fact]
    public void IsValid_ServiceCACert_ReturnsTrue()
    {
        string FindCert(string rel)
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                var candidate = dir;
                for (int j = 0; j < i; j++) candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(candidate, ".."));
                var p = System.IO.Path.Combine(candidate, rel);
                if (System.IO.File.Exists(p)) return p;
            }
            return rel; // last resort
        }

        var certPath = FindCert(System.IO.Path.Combine("certs", "service-ca.crt"));
        var cert = CertificateHelper.LoadCertificate(certPath);
        Assert.NotNull(cert);
        Assert.True(CertificateHelper.IsValid(cert!));
    }
}
