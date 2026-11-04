namespace SimpleWebAppReact.Entities;

public record UserOut(string? Id, string? Username, string? Name, string? PhoneNumber, int? AccountType)
{
    public static UserOut From(User user) => new(user.Id, user.Username, user.Name, user.PhoneNumber, user.AccountType);
}
