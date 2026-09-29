namespace MiCarroAlDia.Domain.Entities;

public class Workshop
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public string Phone { get; private set; }
    public string Address { get; private set; }
    public string City { get; private set; }

    private Workshop() { Id = null!; Name = null!; Phone = null!; Address = null!; City = null!; }

    public Workshop(string id, string name, string phone, string address, string city)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Id = id;
        Name = name;
        Phone = phone;
        Address = address;
        City = city;
    }
}
