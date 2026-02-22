using System;
using System.Linq;
using Oibnovi.Models;
using Oibnovi.Services;
using Xunit;

namespace Oibnovi.Tests;

public class BankServiceTests
{
    [Fact]
    public void AuthenticateUser_WithValidAdminCertificate_ReturnsTrue()
    {
        string FindFile(string rel)
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                var candidate = dir;
                for (int j = 0; j < i; j++) candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(candidate, ".."));
                var p = System.IO.Path.Combine(candidate, rel);
                if (System.IO.File.Exists(p)) return p;
            }
            return rel;
        }

        var serviceCertPath = FindFile(System.IO.Path.Combine("certs", "service-ca.crt"));

        // Ensure a users.json with admin exists so BankService seeds correctly when run under tests
        var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        if (!System.IO.File.Exists(System.IO.Path.Combine(repoRoot, "users.json")))
        {
            var adminCertRel = System.IO.Path.Combine("certs", "admin.crt");
            var adminObj = "{\"admin\":{\"Username\":\"admin\",\"Group\":\"Sluzbenik\",\"Cert\":{\"CertPath\":\"" + adminCertRel.Replace("\\", "\\\\") + "\"}}}";
            System.IO.File.WriteAllText(System.IO.Path.Combine(repoRoot, "users.json"), adminObj);
        }

        // Ensure repository root is current working directory so FileRepository finds users.json
        var repoRootDir = System.IO.Path.GetDirectoryName(serviceCertPath) ?? ".";
        System.IO.Directory.SetCurrentDirectory(repoRootDir + System.IO.Path.DirectorySeparatorChar + "..");
        var svc = new BankService(new Certificate { CertPath = serviceCertPath });

        var admin = svc.GetPersistedUser("admin");
        Assert.NotNull(admin);

        // Ensure admin cert loads
        admin!.Cert.CertPath = FindFile(System.IO.Path.Combine("certs", "admin.crt"));
        Assert.True(admin.Cert.Load());

        // Authentication should succeed
        var ok = svc.AuthenticateUser(new User { Username = "admin", Group = admin.Group, Cert = admin.Cert });
        Assert.True(ok);
    }
}
