using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Oibnovi.Models;
using Oibnovi.Persistence;
using Oibnovi.Utilities;

namespace Oibnovi.Services;

public class BankService
{
    private readonly Dictionary<string, Account> _accounts;
    private readonly List<TransactionRecord> _transactions;
    private readonly Dictionary<string, User> _users;
    private readonly FileRepository _repo = new();
    private readonly Certificate _serviceCert;
    private X509Certificate2? _serviceCertX509;
    private readonly string _eventsLogPath = "events.log";
    private readonly string _transactionsLogPath = "transactions.log";

    public BankService(Certificate serviceCert)
    {
        _serviceCert = serviceCert;

        // Load service CA certificate from file
        if (_serviceCert.Load())
        {
            _serviceCertX509 = _serviceCert.GetX509Cert();
            LogEvent("Service CA certificate loaded successfully");
        }
        else
        {
            LogEvent("WARNING: Service CA certificate failed to load from: " + _serviceCert.CertPath);
        }

        _accounts = _repo.LoadAccounts();
        _transactions = _repo.LoadTransactions();
        _users = _repo.LoadUsers();
        if (_users.Count == 0)
        {
            // Seed default administrator derived from X.509 certificate
            var adminCert = new Certificate { CertPath = "certs/admin.crt" };
            if (adminCert.Load())
            {
                var group = adminCert.GetSubjectOU() ?? "Sluzbenik";
                var admin = new User { Username = "admin", Group = group, Cert = adminCert };
                _users[admin.Username] = admin;
                try { _repo.SaveUsers(_users); } catch { }
            }
        }
    }

    public Dictionary<string, User> ListUsers(User caller)
    {
        if (caller == null) return new Dictionary<string, User>();
        if (caller.Group != "Sluzbenik") return new Dictionary<string, User>();
        return new Dictionary<string, User>(_users);
    }

    public bool AddUser(User caller, User newUser)
    {
        if (caller == null || caller.Group != "Sluzbenik") return false;
        if (string.IsNullOrWhiteSpace(newUser.Username)) return false;
        if (_users.ContainsKey(newUser.Username)) return false;
        _users[newUser.Username] = newUser;
        try { _repo.SaveUsers(_users); } catch { }
        return true;
    }

    public bool RemoveUser(User caller, string username)
    {
        if (caller == null || caller.Group != "Sluzbenik") return false;
        var ok = _users.Remove(username);
        if (ok) try { _repo.SaveUsers(_users); } catch { }
        return ok;
    }

    public User? GetPersistedUser(string username)
    {
        if (string.IsNullOrEmpty(username)) return null;
        _users.TryGetValue(username, out var u);
        return u;
    }

    public List<string> GetAllUsernames()
    {
        return new List<string>(_users.Keys);
    }

    private void LogEvent(string message)
    {
        var line = $"[{DateTime.Now:O}] {message}";
        File.AppendAllText(_eventsLogPath, line + Environment.NewLine);
    }

    private void LogTransaction(TransactionRecord tr)
    {
        _transactions.Add(tr);
        var line = $"[{tr.Time:O}] {tr.Username} {tr.Type} {tr.Amount} Success={tr.Success} Reason={tr.Reason}";
        File.AppendAllText(_transactionsLogPath, line + Environment.NewLine);
        try { _repo.SaveTransactions(_transactions); } catch { }
    }

    /// <summary>
    /// Authenticate a user by validating their X.509 certificate.
    /// Returns true if auth succeeds, false otherwise.
    /// </summary>
    public bool AuthenticateUser(User client)
    {
        return ValidateCertificates(client, out _);
    }

    private bool ValidateCertificates(User client, out string failReason)
    {
        // Validate using real X.509 certificates
        if (client == null || client.Cert == null)
        {
            failReason = "Client has no certificate";
            LogEvent($"Auth failure for {client?.Username ?? "unknown"}: {failReason}");
            return false;
        }

        // Load client certificate from file if not already loaded
        if (client.Cert.GetX509Cert() == null)
        {
            if (!client.Cert.Load())
            {
                failReason = "Failed to load client certificate file";
                LogEvent($"Auth failure for {client.Username}: {failReason}");
                return false;
            }
        }

        // Check if cert is valid (not expired)
        if (!client.Cert.IsValid())
        {
            failReason = "Certificate expired or not yet valid";
            LogEvent($"Auth failure for {client.Username}: {failReason}");
            return false;
        }

        // Validate issuer match: client cert must be issued by service CA
        if (_serviceCertX509 == null)
        {
            failReason = "Service CA certificate not loaded";
            LogEvent($"Auth failure for {client.Username}: {failReason}");
            return false;
        }

        if (!CertificateHelper.ValidateIssuer(client.Cert.GetX509Cert(), _serviceCertX509))
        {
            failReason = "Certificate issuer mismatch";
            LogEvent($"Auth failure for {client.Username}: {failReason}");
            return false;
        }

        // Verify extracted CN matches username
        var certCN = client.Cert.GetSubjectCN();
        if (certCN != client.Username)
        {
            failReason = $"Certificate CN ({certCN}) does not match username ({client.Username})";
            LogEvent($"Auth failure for {client.Username}: {failReason}");
            return false;
        }

        // Verify extracted OU matches group
        var certOU = client.Cert.GetSubjectOU();
        if (certOU != client.Group)
        {
            failReason = $"Certificate OU ({certOU}) does not match group ({client.Group})";
            LogEvent($"Auth failure for {client.Username}: {failReason}");
            return false;
        }

        failReason = string.Empty;
        LogEvent($"Auth success for {client.Username} (X.509 validated)");
        return true;
    }

    public bool OpenAccount(User caller, string username)
    {
        if (!ValidateCertificates(caller, out var vr)) return false;

        if (caller.Group != "Sluzbenik")
            return false;

        if (_accounts.ContainsKey(username))
            return false;

        var acc = new Account
        {
            Number = DateTime.UtcNow.Ticks, // simple unique
            Balance = 0,
            AllowedMinus = 100.0,
            Blocked = false,
            LastTransaction = DateTime.UtcNow,
            OwnerUsername = username
        };

        _accounts[username] = acc;
        try { _repo.SaveAccounts(_accounts); } catch { }
        return true;
    }

    public bool CloseAccount(User caller, string username)
    {
        if (!ValidateCertificates(caller, out var vr)) return false;

        if (caller.Group != "Sluzbenik")
            return false;

        var ok = _accounts.Remove(username);
        if (ok) try { _repo.SaveAccounts(_accounts); } catch { }
        return ok;
    }

    public (bool ok, Account? acc) CheckBalance(User caller, string username)
    {
        if (!ValidateCertificates(caller, out var vr)) return (false, null);

        if (!_accounts.TryGetValue(username, out var acc))
            return (false, null);

        if (caller.Group == "Korisnik" && caller.Username != username)
        {
            LogEvent($"Authorization failure: {caller.Username} tried to read {username} balance");
            return (false, null);
        }

        return (true, acc);
    }

    public void Uplata(User caller, string username, double amount)
    {
        if (!ValidateCertificates(caller, out var vr))
        {
            var trfail = new TransactionRecord { Time = DateTime.UtcNow, Username = username, Type = "Uplata", Amount = amount, Success = false, Reason = $"Auth failed: {vr}" };
            LogTransaction(trfail);
            return;
        }

        if (caller.Group == "Korisnik" && caller.Username != username)
        {
            var trfail = new TransactionRecord { Time = DateTime.UtcNow, Username = username, Type = "Uplata", Amount = amount, Success = false, Reason = "Permission denied" };
            LogTransaction(trfail);
            return;
        }
        var tr = new TransactionRecord { Time = DateTime.UtcNow, Username = username, Type = "Uplata", Amount = amount };
        if (!_accounts.TryGetValue(username, out var acc))
        {
            tr.Success = false; tr.Reason = "Account not found"; LogTransaction(tr); return;
        }

        var prev = acc.Balance;
        acc.Balance += amount;
        acc.LastTransaction = DateTime.UtcNow;

        if (prev < 0 && acc.Balance >= 0 && acc.Blocked)
        {
            acc.Blocked = false;
        }

        tr.Success = true;
        LogTransaction(tr);
        try { _repo.SaveAccounts(_accounts); } catch { }
    }

    public void Isplata(User caller, string username, double amount)
    {
        if (!ValidateCertificates(caller, out var vr))
        {
            var trfail = new TransactionRecord { Time = DateTime.UtcNow, Username = username, Type = "Isplata", Amount = amount, Success = false, Reason = $"Auth failed: {vr}" };
            LogTransaction(trfail);
            return;
        }

        if (caller.Group == "Korisnik" && caller.Username != username)
        {
            var trfail = new TransactionRecord { Time = DateTime.UtcNow, Username = username, Type = "Isplata", Amount = amount, Success = false, Reason = "Permission denied" };
            LogTransaction(trfail);
            return;
        }
        var tr = new TransactionRecord { Time = DateTime.UtcNow, Username = username, Type = "Isplata", Amount = amount };
        if (!_accounts.TryGetValue(username, out var acc))
        {
            tr.Success = false; tr.Reason = "Account not found"; LogTransaction(tr); return;
        }

        if (acc.Blocked)
        {
            tr.Success = false; tr.Reason = "Account blocked"; LogTransaction(tr); return;
        }

        if (acc.Balance - amount < -acc.AllowedMinus)
        {
            tr.Success = false; tr.Reason = "Exceeds allowed minus"; LogTransaction(tr); return;
        }

        acc.Balance -= amount;
        acc.LastTransaction = DateTime.UtcNow;
        tr.Success = true;
        LogTransaction(tr);
        try { _repo.SaveAccounts(_accounts); } catch { }
    }

    public bool Opomena(User caller, string username)
    {
        if (!ValidateCertificates(caller, out var vr)) return false;

        if (caller.Group != "Sluzbenik")
            return false;

        if (!_accounts.TryGetValue(username, out var acc))
            return false;

        if (acc.Balance < 0)
        {
            acc.Blocked = true;
            try { _repo.SaveAccounts(_accounts); } catch { }
            return true;
        }

        return false;
    }
}
