using System;
using Oibnovi.Models;
using Oibnovi.Services;

Console.WriteLine("OIBNOVI Console App — bank system demo");

var serviceCert = new Certificate { Issuer = "BANK-CA", SubjectCN = "OIBService", SubjectOU = "Service" };
var svc = new BankService(serviceCert);

var sluzbenik = new User { Username = "admin", Group = "Službenik", Cert = new Certificate { Issuer = "BANK-CA", SubjectCN = "admin", SubjectOU = "Službenik" } };
var korisnik = new User { Username = "ivan", Group = "Korisnik", Cert = new Certificate { Issuer = "BANK-CA", SubjectCN = "ivan", SubjectOU = "Korisnik" } };

Console.WriteLine("Running demo sequence...");

if (svc.OpenAccount(sluzbenik, korisnik.Username))
    Console.WriteLine("Account opened for ivan");

svc.Uplata(korisnik, korisnik.Username, 50);
svc.Isplata(korisnik, korisnik.Username, 20);

var (ok, acc) = svc.CheckBalance(korisnik, korisnik.Username);
if (ok && acc != null)
    Console.WriteLine($"Balance for {acc.OwnerUsername}: {acc.Balance}, Blocked={acc.Blocked}");

svc.Isplata(korisnik, korisnik.Username, 200); // should go into minus but within allowed minus

if (svc.Opomena(sluzbenik, korisnik.Username))
    Console.WriteLine("Opomena applied (account possibly blocked)");

var (ok2, acc2) = svc.CheckBalance(sluzbenik, korisnik.Username);
if (ok2 && acc2 != null)
    Console.WriteLine($"Final: {acc2.OwnerUsername}: {acc2.Balance}, Blocked={acc2.Blocked}");

Console.WriteLine("Demo finished. Logs: events.log, transactions.log");

