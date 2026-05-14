using Application.Services.Egypt;

namespace Tests.Egypt;

public class EtaCanonicalSerializerTests
{
    [Fact]
    public void Serialize_FlatObject_UppercasesPropertyNames()
    {
        var doc = new { name = "Acme", trn = "100" };
        var result = EtaCanonicalSerializer.Serialize(doc);
        result.Should().Be("\"NAME\"\"Acme\"\"TRN\"\"100\"");
    }

    [Fact]
    public void Serialize_NumericValue_IsWrappedInQuotes()
    {
        var doc = new { total = 12.5m };
        EtaCanonicalSerializer.Serialize(doc).Should().Be("\"TOTAL\"\"12.5\"");
    }

    [Fact]
    public void Serialize_NestedObject_FlattensWithUppercaseKeys()
    {
        var doc = new { issuer = new { id = "100", name = "X" } };
        EtaCanonicalSerializer.Serialize(doc)
            .Should().Be("\"ISSUER\"\"ID\"\"100\"\"NAME\"\"X\"");
    }

    [Fact]
    public void Serialize_Array_ConcatenatesElementSerializations()
    {
        var doc = new
        {
            lines = new object[]
            {
                new { qty = 1m },
                new { qty = 2m }
            }
        };
        EtaCanonicalSerializer.Serialize(doc)
            .Should().Be("\"LINES\"\"QTY\"\"1\"\"QTY\"\"2\"");
    }

    [Fact]
    public void Serialize_NullValue_RendersAsEmptyString()
    {
        var doc = new { note = (string?)null };
        EtaCanonicalSerializer.Serialize(doc).Should().Be("\"NOTE\"\"\"");
    }

    [Fact]
    public void Serialize_Boolean_StringifiesValue()
    {
        var doc = new { active = true, inactive = false };
        EtaCanonicalSerializer.Serialize(doc)
            .Should().Be("\"ACTIVE\"\"true\"\"INACTIVE\"\"false\"");
    }
}
