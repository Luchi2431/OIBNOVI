using System;
using System.Collections.Generic;
using System.IO;
using Oibnovi.Models;
using Oibnovi.Persistence;

namespace Oibnovi.Services;

public class BankService
{
    private readonly Dictionary<string, Account> _accounts;
    private readonly List<TransactionRecord> _transactions;
    private readonly FileRepository _repo = new();
    private readonly Certificate _serviceCert;
    private readonly string _eventsLogPath = "events.log";
    private readonly string _transactionsLogPath = "transactions.log";

    public BankService(Certificate serviceCert)
    {
        _serviceCert = serviceCert;
        _accounts = _repo.LoadAccounts();
        _transactions = _repo.LoadTransactions();
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

    private bool ValidateCertificates(User client, out string failReason)
    {
        // As per spec: both cert issuers must match
        if (client.Cert == null)
        {
            failReason = "Client has no certificate";
            LogEvent($"Auth failure for {client.Username}: {failReason}");
            return false;
        }

        if (_serviceCert.Issuer != client.Cert.Issuer)
        {
            failReason = "Certificate issuer mismatch";
            LogEvent($"Auth failure for {client.Username}: {failReason}");
            return false;
        }

        failReason = string.Empty;
        LogEvent($"Auth success for {client.Username}");
        return true;
    }

    public bool OpenAccount(User caller, string username)
    {
        if (caller.Group != "Službenik")
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
        if (caller.Group != "Službenik")
            return false;

        var ok = _accounts.Remove(username);
        if (ok) try { _repo.SaveAccounts(_accounts); } catch { }
        return ok;
    }

    public (bool ok, Account? acc) CheckBalance(User caller, string username)
    {
        if (!_accounts.TryGetValue(username, out var acc))
            return (false, null);

        return (true, acc);
    }

    public void Uplata(User caller, string username, double amount)
    {
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
        if (caller.Group != "Službenik")
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
