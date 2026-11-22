using FluentAssertions;
using ImportaYa.Domain.Enums;
using ImportaYa.Domain.Rules;
using Xunit;

namespace ImportaYa.Domain.Tests;

public sealed class DocumentFormatRulesTests
{
    [Theory]
    [InlineData(TipoDocumento.CC, "1045678901", true)]
    [InlineData(TipoDocumento.CE, "1045678901", true)]
    [InlineData(TipoDocumento.TI, "1045678901", true)]
    [InlineData(TipoDocumento.CC, "ABC123", false)]
    [InlineData(TipoDocumento.CC, "1045-678", false)]
    [InlineData(TipoDocumento.PAS, "AB123456", true)]
    [InlineData(TipoDocumento.PAS, "AB-123", false)]
    [InlineData(TipoDocumento.NIT, "900123456-7", true)]
    [InlineData(TipoDocumento.NIT, "900123456", true)]
    [InlineData(TipoDocumento.NIT, "900123456-78", false)]
    [InlineData(TipoDocumento.NIT, "90A123456-7", false)]
    [InlineData(TipoDocumento.CC, "", false)]
    [InlineData(TipoDocumento.CC, "   ", false)]
    public void Validate_document_format(TipoDocumento tipo, string documento, bool expected)
    {
        DocumentFormatRules.IsValid(tipo, documento).Should().Be(expected);
    }

    [Fact]
    public void Leading_zeros_are_valid_for_numeric_documents()
    {
        DocumentFormatRules.IsValid(TipoDocumento.CC, "0001234567").Should().BeTrue();
    }
}
