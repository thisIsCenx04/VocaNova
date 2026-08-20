namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
