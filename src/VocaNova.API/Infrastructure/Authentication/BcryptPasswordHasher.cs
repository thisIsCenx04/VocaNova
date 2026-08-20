using VocaNova.API.Common.Security;
using VocaNova.API.Features.Auth.BLL.Abstractions;

namespace VocaNova.API.Infrastructure.Authentication;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => PasswordHelper.Hash(password);

    public bool Verify(string password, string passwordHash) => PasswordHelper.Verify(password, passwordHash);
}
