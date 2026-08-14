using DartERP.Core.Security;
using Xunit;

namespace DartERP.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("CorrectHorseBattery1");

        Assert.True(PasswordHasher.Verify("CorrectHorseBattery1", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("CorrectHorseBattery1");

        Assert.False(PasswordHasher.Verify("WrongPassword1", hash));
    }

    [Fact]
    public void Hash_CalledTwiceForSamePassword_ProducesDifferentHashes()
    {
        var first = PasswordHasher.Hash("SamePassword1");
        var second = PasswordHasher.Hash("SamePassword1");

        Assert.NotEqual(first, second);
    }
}
