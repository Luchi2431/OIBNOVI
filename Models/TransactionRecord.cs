using System;

namespace Oibnovi.Models;

public class TransactionRecord
{
    public DateTime Time { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Uplata, Isplata
    public double Amount { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
}
