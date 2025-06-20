namespace Models
{
    public class NetstatEntry
    {
        public string Protocol { get; set; } = string.Empty;
        public string LocalAddress { get; set; } = string.Empty;
        public string ForeignAddress { get; set; } = string.Empty;
        public string? State { get; set; }
        public int PID { get; set; }
        public string ProcessName { get; set; } = string.Empty;
    }
}