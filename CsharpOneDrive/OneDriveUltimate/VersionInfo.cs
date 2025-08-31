public class VersionInfo
{
    public string Version { get; set; }
    public string VersionDate { get; set; }
        
        public override bool Equals(object obj)
    {
        if (obj is VersionInfo other)
        {
            return this.VersionDate == other.VersionDate && this.Version == other.Version;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(VersionDate, Version);
    }
    }