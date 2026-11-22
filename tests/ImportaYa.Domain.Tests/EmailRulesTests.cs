using FluentAssertions;
using ImportaYa.Domain.Rules;
using Xunit;

namespace ImportaYa.Domain.Tests;

public sealed class EmailRulesTests
{
    [Theory]
    [InlineData("ana@demo.com", true)]
    [InlineData("ana.perez@sub.demo.co", true)]
    [InlineData("correo-mal", false)]
    [InlineData("ana@", false)]
    [InlineData("@demo.com", false)]
    [InlineData("ana demo@correo.com", false)]
    public void Validate_email(string email, bool expected)
    {
        EmailRules.IsValid(email).Should().Be(expected);
    }
}
