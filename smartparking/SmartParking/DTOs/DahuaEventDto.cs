namespace SmartParking.DTOs
{
    /// <summary>
    /// DTO for incoming ANPR event from Dahua DSS webhook
    /// Based on DSS "Event Transferal" JSON format
    /// </summary>
    public class DahuaEventDto
    {
        public string? EventId { get; set; }
        public string? EventType { get; set; }  // VehicleDetection, ANPR
        public string? Direction { get; set; }   // Enter, Exit
        public string? PlateNumber { get; set; }
        public string? PlateCountry { get; set; }
        public int? Confidence { get; set; }
        public string? SnapshotUrl { get; set; }
        public string? ChannelId { get; set; }
        public string? ChannelName { get; set; }
        public DateTime? EventTime { get; set; }

        // Nested Dahua structure
        public DahuaEventDetail? Detail { get; set; }
        public DahuaEventVehicle? Vehicle { get; set; }
    }

    public class DahuaEventDetail
    {
        public string? PlateNumber { get; set; }
        public string? PlateColor { get; set; }
        public string? VehicleColor { get; set; }
        public string? VehicleType { get; set; }
        public string? Speed { get; set; }
        public string? SnapshotUrl { get; set; }
    }

    public class DahuaEventVehicle
    {
        public string? PlateNumber { get; set; }
        public string? PlateCountry { get; set; }
        public int? Confidence { get; set; }
    }

    /// <summary>
    /// DTO for sending barrier open command to Dahua DSS
    /// </summary>
    public class BarrierCommandDto
    {
        public string ChannelId { get; set; } = string.Empty;
        public int BarrierChannel { get; set; } = 1;
        public string Action { get; set; } = "open"; // open, close, pulse
    }

    /// <summary>
    /// DTO for parking space status sent to DSS
    /// </summary>
    public class ParkingSpaceStatusDto
    {
        public int TotalSpaces { get; set; }
        public int OccupiedSpaces { get; set; }
        public int FreeSpaces => TotalSpaces - OccupiedSpaces;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
