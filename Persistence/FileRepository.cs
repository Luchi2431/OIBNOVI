using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Oibnovi.Models;

namespace Oibnovi.Persistence;

public class FileRepository
{
    private readonly string _accountsPath = "accounts.json";
    private readonly string _transactionsPath = "transactions.json";
    private readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.General) { WriteIndented = true };

    public Dictionary<string, Account> LoadAccounts()
    {
        if (!File.Exists(_accountsPath)) return new Dictionary<string, Account>();
        var txt = File.ReadAllText(_accountsPath);
        return JsonSerializer.Deserialize<Dictionary<string, Account>>(txt, _opts) ?? new Dictionary<string, Account>();
    }

    public void SaveAccounts(Dictionary<string, Account> accounts)
    {
        var tmp = _accountsPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(accounts, _opts));
        File.Move(tmp, _accountsPath, true);
    }

    public List<TransactionRecord> LoadTransactions()
    {
        if (!File.Exists(_transactionsPath)) return new List<TransactionRecord>();
        var txt = File.ReadAllText(_transactionsPath);
        return JsonSerializer.Deserialize<List<TransactionRecord>>(txt, _opts) ?? new List<TransactionRecord>();
    }

    public void SaveTransactions(List<TransactionRecord> transactions)
    {
        var tmp = _transactionsPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(transactions, _opts));
        File.Move(tmp, _transactionsPath, true);
    }
}
