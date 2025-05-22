using System;
using System.Collections.Generic;

namespace Base.Models.DTOs.Device
{
    public class DeviceReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class DeviceDetailDto : DeviceReadDto
    {
        public List<Models.DTOs.DeviceLog.DeviceLogReadDto> Logs { get; set; } = new List<Models.DTOs.DeviceLog.DeviceLogReadDto>();
    }
} 