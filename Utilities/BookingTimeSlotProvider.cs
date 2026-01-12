using System.Collections.Generic;

namespace KD_Restaurant.Utilities
{
    public static class BookingTimeSlotProvider
    {
        private static readonly IReadOnlyList<string> _defaultSlots = new List<string>
        {
            "Sáng 07:00 - 09:00",
            "Sáng 09:00 - 11:00",
            "Trưa 11:00 - 13:00",
            "Chiều 13:00 - 17:00",
            "Tối 17:00 - 19:00",
            "Tối 19:00 - 21:00"
        };

        public static IReadOnlyList<string> GetDefaultSlots() => _defaultSlots;
    }
}
