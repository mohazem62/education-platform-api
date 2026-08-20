using EducationPlatform.Application;

namespace EducationPlatform.UnitTests;
public sealed class PhoneNormalizerTests
{
    [Theory] [InlineData("+201012345678", "+201012345678")] [InlineData("01012345678", "01012345678")] [InlineData("+966 50 123 4567", "+966501234567")] [InlineData("971-50-123-4567", "971501234567")] public void International_numbers_are_normalized_without_country_specific_rules(string input, string expected) => Assert.Equal(expected, PhoneNormalizer.Normalize(input));
    [Fact] public void Sensitive_destination_is_masked() => Assert.Equal("01******678", PhoneNormalizer.Mask("01012345678"));
}
