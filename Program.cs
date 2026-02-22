using System;
using Oibnovi.Models;
using Oibnovi.Services;

Console.WriteLine("OIBNOVI Console App — interactive CLI with X.509 certificates");

var serviceCert = new Certificate { CertPath = "certs/service-ca.crt" };
var svc = new BankService(serviceCert);

User? current = null;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("Current user: " + (current?.Username ?? "(none)"));
    Console.WriteLine("Choose action:");
    Console.WriteLine("1) Login");
    Console.WriteLine("2) Logout");
    Console.WriteLine("3) OpenAccount");
    Console.WriteLine("4) CloseAccount");
    Console.WriteLine("5) CheckBalance");
    Console.WriteLine("6) Uplata (deposit)");
    Console.WriteLine("7) Isplata (withdraw)");
    Console.WriteLine("8) Opomena (block if negative)");
    Console.WriteLine("9) List accounts");
    Console.WriteLine("10) Create user (Sluzbenik only)");
    Console.WriteLine("11) Delete user (Sluzbenik only)");
    Console.WriteLine("12) List users");
    Console.WriteLine("0) Exit");
    Console.Write("> ");
    var cmd = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(cmd)) continue;

    if (cmd == "0") break;
    if (cmd == "1")
    {
        // X.509 certificate-based login
        Console.WriteLine("Enter username (or press Enter to list users): ");
        var u = Console.ReadLine() ?? "";
        if (string.IsNullOrWhiteSpace(u))
        {
            var names = svc.GetAllUsernames();
            if (names.Count == 0) { Console.WriteLine("No users available."); continue; }
            for (var i = 0; i < names.Count; i++) Console.WriteLine($"{i + 1}) {names[i]}");
            Console.Write("Pick user number: ");
            var pick = Console.ReadLine();
            if (!int.TryParse(pick, out var idx) || idx < 1 || idx > names.Count) { Console.WriteLine("Invalid choice"); continue; }
            u = names[idx - 1];
            Console.WriteLine($"Selected user: {u}");
        }

        var persisted = svc.GetPersistedUser(u);
        if (persisted == null)
        {
            Console.WriteLine("User not found. Please ask a Sluzbenik to create the user first.");
            continue;
        }

        // Show expected cert path and allow override
        var defaultCertPath = $"certs/{u}.crt";
        Console.Write($"Cert file path (press Enter to use '{defaultCertPath}'): ");
        var certPath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(certPath)) certPath = defaultCertPath;

        // Load certificate and authenticate
        persisted.Cert.CertPath = certPath;
        if (!persisted.Cert.Load())
        {
            Console.WriteLine($"Failed to load certificate from {certPath}");
            continue;
        }

        // Authenticate via X.509 certificate validation
        var testUser = new User { Username = u, Group = persisted.Group, Cert = persisted.Cert };
        if (!svc.AuthenticateUser(testUser))
        {
            Console.WriteLine("Certificate validation failed. Check events.log for details.");
            continue;
        }

        current = persisted;
        Console.WriteLine($"Logged in as {current.Username} ({current.Group}) with X.509 certificate");
        continue;
    }

    if (cmd == "2") { current = null; Console.WriteLine("Logged out"); continue; }

    if (cmd == "12")
    {
        var names = svc.GetAllUsernames();
        if (names.Count == 0) Console.WriteLine("No users"); else { Console.WriteLine("Available users:"); foreach (var n in names) Console.WriteLine($"  - {n}"); }
        continue;
    }

    if (current == null) { Console.WriteLine("Please login first"); continue; }

    switch (cmd)
    {
        case "3":
            {
                Console.Write("Open account for username: ");
                var ou = Console.ReadLine() ?? "";
                var ok = svc.OpenAccount(current, ou);
                Console.WriteLine(ok ? "Account opened" : "Failed to open account");
                break;
            }
        case "4":
            {
                Console.Write("Close account for username: ");
                var cu = Console.ReadLine() ?? "";
                var okc = svc.CloseAccount(current, cu);
                Console.WriteLine(okc ? "Account closed" : "Failed to close account");
                break;
            }
        case "5":
            {
                Console.Write("Check balance for username: ");
                var bu = Console.ReadLine() ?? "";
                var (okb, acc) = svc.CheckBalance(current, bu);
                if (!okb) Console.WriteLine("Cannot read balance or account not found"); else Console.WriteLine($"{acc.OwnerUsername}: {acc.Balance} Blocked={acc.Blocked}");
                break;
            }
        case "6":
            {
                Console.Write("Deposit to username: ");
                var du = Console.ReadLine() ?? "";
                Console.Write("Amount: ");
                if (double.TryParse(Console.ReadLine(), out var damt)) { svc.Uplata(current, du, damt); Console.WriteLine("Deposit performed (or logged failure)"); } else Console.WriteLine("Invalid amount");
                break;
            }
        case "7":
            {
                Console.Write("Withdraw from username: ");
                var wu = Console.ReadLine() ?? "";
                Console.Write("Amount: ");
                if (double.TryParse(Console.ReadLine(), out var wamt)) { svc.Isplata(current, wu, wamt); Console.WriteLine("Withdraw attempted (or logged failure)"); } else Console.WriteLine("Invalid amount");
                break;
            }
        case "8":
            {
                Console.Write("Opomena for username: ");
                var ou2 = Console.ReadLine() ?? "";
                var op = svc.Opomena(current, ou2);
                Console.WriteLine(op ? "Opomena applied" : "Opomena not applied");
                break;
            }
        case "9":
            Console.WriteLine("Accounts (from accounts.json):");
            try { Console.WriteLine(System.IO.File.ReadAllText("accounts.json")); } catch { Console.WriteLine("No accounts.json or read error"); }
            break;
        case "10":
            {
                Console.WriteLine("Create user");
                if (current == null || current.Group != "Sluzbenik") { Console.WriteLine("Only Sluzbenik can create users"); break; }
                Console.Write("New username: ");
                var nu = Console.ReadLine() ?? "";
                Console.Write("Group (Sluzbenik/Korisnik): ");
                var ng = Console.ReadLine() ?? "Korisnik";
                Console.Write("Certificate file path (e.g., certs/user.crt): ");
                var certPath = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(certPath)) certPath = $"certs/{nu}.crt";

                // Validate cert file exists and has correct CN/OU
                var cert = new Certificate { CertPath = certPath };
                if (!cert.Load())
                {
                    Console.WriteLine($"Failed to load certificate from {certPath}");
                    break;
                }

                var certCN = cert.GetSubjectCN();
                var certOU = cert.GetSubjectOU();
                if (certCN != nu)
                {
                    Console.WriteLine($"Certificate CN ({certCN}) must match username ({nu})");
                    break;
                }
                if (certOU != ng)
                {
                    Console.WriteLine($"Certificate OU ({certOU}) must match group ({ng})");
                    break;
                }

                var newUser = new User { Username = nu, Group = ng, Cert = cert };
                var added = svc.AddUser(current, newUser);
                Console.WriteLine(added ? "User created" : "Failed to create user (exists or permission)");
                break;
            }
        case "11":
            {
                Console.WriteLine("Delete user");
                if (current == null || current.Group != "Sluzbenik") { Console.WriteLine("Only Sluzbenik can delete users"); break; }
                Console.Write("Username to delete: ");
                var delUser = Console.ReadLine() ?? "";
                var removed = svc.RemoveUser(current, delUser);
                Console.WriteLine(removed ? "User removed" : "Failed to remove user");
                break;
            }
        default:
            Console.WriteLine("Unknown command");
            break;
    }
}

Console.WriteLine("Exiting CLI");

