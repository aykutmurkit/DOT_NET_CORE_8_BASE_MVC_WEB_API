using System;

namespace Base.Models.DTOs.DeviceLog
{
    public class DeviceLogReadDto
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Message { get; set; }
        public string LogType { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Severity { get; set; }
        public bool IsResolved { get; set; }
        public string ResolutionNotes { get; set; }
        public DateTime? ResolvedDate { get; set; }
    }
} 