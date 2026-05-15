using Application.Services.Security;

namespace Tests.Security;

public class TotpServiceTests
{
    [Fact]
    public void GenerateSecret_ReturnsBase32String()
    {
        var s = TotpService.GenerateSecret();
        s.Should().NotBeNullOrWhiteSpace();
        s.Length.Should().BeGreaterThanOrEqualTo(30);
        // Only base32 chars (A-Z, 2-7) allowed
        s.Should().MatchRegex(@"^[A-Z2-7]+$");
    }

    [Fact]
    public void Base32_RoundTrip()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x11, 0x22 };
        var encoded = TotpService.Base32Encode(bytes);
        var decoded = TotpService.Base32Decode(encoded);
        decoded.Should().Equal(bytes);
    }

    [Fact]
    public void BuildOtpAuthUri_ContainsExpectedParts()
    {
        var uri = TotpService.BuildOtpAuthUri("ErpApi", "admin", "JBSWY3DPEHPK3PXP");
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("ErpApi:admin");
        uri.Should().Contain("secret=JBSWY3DPEHPK3PXP");
        uri.Should().Contain("digits=6");
        uri.Should().Contain("period=30");
    }

    [Fact]
    public void Verify_RejectsObviouslyInvalidCodes()
    {
        var secret = TotpService.GenerateSecret();
        TotpService.Verify(secret, "").Should().BeFalse();
        TotpService.Verify(secret, "abc123").Should().BeFalse();
        TotpService.Verify(secret, "12345").Should().BeFalse();   // too short
        TotpService.Verify(secret, "1234567").Should().BeFalse(); // too long
    }

    [Fact]
    public void Verify_RejectsRandomCodeWithVeryHighProbability()
    {
        // Statistical sanity check — a fixed unrelated code should not match
        // a random secret. This isn't cryptographically meaningful but catches
        // obvious bugs (e.g. always returning true).
        var secret = TotpService.GenerateSecret();
        var rejects = 0;
        for (var i = 0; i < 100; i++)
        {
            if (!TotpService.Verify(secret, i.ToString().PadLeft(6, '0'))) rejects++;
        }
        rejects.Should().BeGreaterThanOrEqualTo(97); // at most ~3 false positives in 100 tries
    }
}
