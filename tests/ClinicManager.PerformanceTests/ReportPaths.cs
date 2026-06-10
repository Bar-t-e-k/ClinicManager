namespace ClinicManager.PerformanceTests;

internal static class ReportPaths
{
    public static string RepoRoot
    {
        get
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "ClinicManager.slnx")))
                    return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }

            return Directory.GetCurrentDirectory();
        }
    }

    public static string NbomberReport => Path.Combine(RepoRoot, "nbomber-report.pdf");
    public static string IndexReport => Path.Combine(RepoRoot, "raport-indeksy.pdf");
    public static string SqlProfilerReport => Path.Combine(RepoRoot, "raport-sql-profiler.pdf");
}
