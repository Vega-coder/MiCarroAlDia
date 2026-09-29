namespace MiCarroAlDia.Tests;

public class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public TestTimeProvider(DateTimeOffset initialUtcNow)
    {
        _utcNow = initialUtcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan timeSpan) => _utcNow = _utcNow.Add(timeSpan);
    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
}
