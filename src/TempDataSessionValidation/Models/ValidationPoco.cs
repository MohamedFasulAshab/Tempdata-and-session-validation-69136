namespace TempDataSessionValidation.Models;

public sealed class ValidationPoco
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public override string ToString()
    {
        return $"Id={Id}, Name={Name}, IsActive={IsActive}";
    }
}