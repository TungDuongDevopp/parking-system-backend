using ParkingSystem.Validation;
using Xunit;

namespace ParkingSystem.Tests.Validation;

public class ValidationAttribute_Tests
{
    [Theory]
    [InlineData("first.last+tag@example.co.uk")]
    [InlineData("user_name@example-domain.com")]
    public void EmailAttribute_ShouldAcceptCommonEmailAddresses(string email)
    {
        Assert.True(new EmailAttribute().IsValid(email));
    }

    [Theory]
    [InlineData("first..last@example.com")]
    [InlineData("first last@example.com")]
    [InlineData("first@example..com")]
    [InlineData("first@-example.com")]
    [InlineData("first@example-.com")]
    [InlineData("first@example.com\n")]
    public void EmailAttribute_ShouldRejectMalformedEmailAddresses(string email)
    {
        Assert.False(new EmailAttribute().IsValid(email));
    }

    [Fact]
    public void EmailAttribute_ShouldAllowEmptyValueForPartialUpdates()
    {
        var attribute = new EmailAttribute();

        Assert.True(attribute.IsValid(null));
        Assert.True(attribute.IsValid(string.Empty));
        Assert.True(attribute.IsValid("  "));
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("0351234567")]
    public void PhoneNumberAttribute_ShouldAcceptSupportedTenDigitNumbers(string phoneNumber)
    {
        Assert.True(new PhoneNumberAttribute().IsValid(phoneNumber));
    }

    [Theory]
    [InlineData("0212345678")]
    [InlineData("091234567")]
    [InlineData("09123456789")]
    [InlineData("091234567\n")]
    [InlineData("٠٩١٢٣٤٥٦٧٨")]
    public void PhoneNumberAttribute_ShouldRejectUnsupportedFormats(string phoneNumber)
    {
        Assert.False(new PhoneNumberAttribute().IsValid(phoneNumber));
    }
}
