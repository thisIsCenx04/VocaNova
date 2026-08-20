namespace VocaNova.API.Features.Auth.BLL.Abstractions;

public interface IRefreshTokenHasher
{
    string Hash(string refreshToken);
}
