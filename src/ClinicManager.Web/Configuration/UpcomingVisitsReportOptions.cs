namespace ClinicManager.Web.Configuration;

public class UpcomingVisitsReportOptions
{
    public const string SectionName = "UpcomingVisitsReport";

    /// <summary>Wyłącz usługę e-mail (np. lokalnie bez Mailtrap): user-secrets Enabled=false.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Interwał pętli usługi w tle (minuty). Dla testów ustaw 1–2.</summary>
    public int IntervalMinutes { get; set; } = 1440;

    /// <summary>Ścieżka względna do generowanego pliku PDF.</summary>
    public string PdfOutputPath { get; set; } = "reports/raport-nadchodzace-wizyty.pdf";

    /// <summary>Adres e-mail administratora (odbiorca raportu).</summary>
    public string AdminEmail { get; set; } = "admin@clinic.com";

    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@clinic.local";
    public string FromDisplayName { get; set; } = "ClinicManager";
}
