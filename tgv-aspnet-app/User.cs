namespace tgv_aspnet_app;

public record User
{
    private static Random _r = new();
    
    public User() : this(Guid.NewGuid().ToString())
    {
    }

    public User(string id) : this(id,
        $"Name_{_r.Next(0, 100)}",
        $"Surname_{_r.Next(0, 100)}",
        _r.Next(0, 100))
    {
    }

    public User(string id, string name, string surname, int age)
    {
        Id = id;
        Name = name;
        Surname = surname;
        Age = age;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public int Age { get; set; }
}