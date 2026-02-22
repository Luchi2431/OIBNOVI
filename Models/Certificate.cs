namespace Oibnovi.Models;

public class Certificate
{
    public string Issuer { get; set; } = string.Empty;
    public string SubjectCN { get; set; } = string.Empty; // username
    public string SubjectOU { get; set; } = string.Empty; // group
}
