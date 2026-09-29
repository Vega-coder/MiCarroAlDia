using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Domain.Entities;

public class VehicleProgressHistory
{
    public VehicleProgressState State { get; private set; }
    public DateTimeOffset TimestampUtc { get; private set; }
    public string Note { get; private set; }

    private VehicleProgressHistory() { Note = null!; }

    public VehicleProgressHistory(VehicleProgressState state, DateTimeOffset timestampUtc, string note)
    {
        State = state;
        TimestampUtc = timestampUtc;
        Note = note;
    }
}
