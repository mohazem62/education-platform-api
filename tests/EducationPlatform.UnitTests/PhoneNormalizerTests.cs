using EducationPlatform.Application;

namespace EducationPlatform.UnitTests;
public sealed class PhoneNormalizerTests
{
    [Theory] [InlineData("+201012345678", "01012345678")] [InlineData("01012345678", "01012345678")] [InlineData("10 123 456 78", "01012345678")] public void Egyptian_numbers_are_normalized(string input, string expected) => Assert.Equal(expected, PhoneNormalizer.Normalize(input));
    [Fact] public void Sensitive_destination_is_masked() => Assert.Equal("01******678", PhoneNormalizer.Mask("01012345678"));
}
