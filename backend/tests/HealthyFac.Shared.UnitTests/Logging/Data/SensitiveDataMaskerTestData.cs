using HealthyFac.Shared.Application.Logging;

namespace HealthyFac.Shared.UnitTests.Logging.Data;

public static class SensitiveDataMaskerTestData
{
    public const string SecretPassword = "SuperSecretPassword123!";
    public const string CreditCardNumber = "4111222233334444";
    public const string ShortSecret = "abc";
    public const string FiveCharSecret = "12345";
    public const string FourCharSecret = "1234";
    public const string CustomMaskString = "REDACTED";
    public const string PreservedLengthCardMask = "************4444";
    public const string ShowLastCardMask = "***MASKED***4444";
    public const string ShowFirstAndLastMask = "41***MASKED***44";
    public const string ThreeAsterisksMask = "***";
    public const string FourAsterisksMask = "****";
    public const string FiveAsterisksMask = "*****";

    public const int PrefixLengthTwo = 2;
    public const int SuffixLengthTwo = 2;
    public const int PrefixLengthThree = 3;
    public const int SuffixLengthThree = 3;
    public const int PrefixLengthFive = 5;
    public const int SuffixLengthFive = 5;
    public const int SuffixLengthFour = 4;

    public static TheoryData<string?, SensitiveDataAttribute, string?> MaskScenariosData => new()
    {
        {
            SecretPassword,
            new SensitiveDataAttribute(),
            MaskingConstants.DefaultMask
        },
        {
            SecretPassword,
            new SensitiveDataAttribute(CustomMaskString),
            CustomMaskString
        },
        {
            CreditCardNumber,
            new SensitiveDataAttribute
            {
                ShowLast = SuffixLengthFour,
            },
            ShowLastCardMask
        },
        {
            CreditCardNumber,
            new SensitiveDataAttribute
            {
                ShowLast = SuffixLengthFour,
                PreserveLength = true,
            },
            PreservedLengthCardMask
        },
        {
            CreditCardNumber,
            new SensitiveDataAttribute
            {
                ShowFirst = PrefixLengthTwo,
                ShowLast = SuffixLengthTwo,
            },
            ShowFirstAndLastMask
        },
        {
            ShortSecret,
            new SensitiveDataAttribute
            {
                ShowFirst = PrefixLengthTwo,
                ShowLast = SuffixLengthTwo,
            },
            MaskingConstants.DefaultMask
        },
        {
            ShortSecret,
            new SensitiveDataAttribute
            {
                ShowFirst = PrefixLengthTwo,
                ShowLast = SuffixLengthTwo,
                PreserveLength = true,
            },
            ThreeAsterisksMask
        },
        {
            FiveCharSecret,
            new SensitiveDataAttribute
            {
                ShowFirst = PrefixLengthThree,
                ShowLast = SuffixLengthThree,
            },
            MaskingConstants.DefaultMask
        },
        {
            FiveCharSecret,
            new SensitiveDataAttribute
            {
                ShowFirst = PrefixLengthThree,
                ShowLast = SuffixLengthThree,
                PreserveLength = true,
            },
            FiveAsterisksMask
        },
        {
            FourCharSecret,
            new SensitiveDataAttribute
            {
                ShowFirst = PrefixLengthTwo,
                ShowLast = SuffixLengthTwo,
            },
            MaskingConstants.DefaultMask
        },
        {
            FourCharSecret,
            new SensitiveDataAttribute
            {
                ShowFirst = PrefixLengthTwo,
                ShowLast = SuffixLengthTwo,
                PreserveLength = true,
            },
            FourAsterisksMask
        },
        {
            ShortSecret,
            new SensitiveDataAttribute
            {
                ShowFirst = PrefixLengthFive,
            },
            MaskingConstants.DefaultMask
        },
        {
            ShortSecret,
            new SensitiveDataAttribute
            {
                ShowLast = SuffixLengthFive,
            },
            MaskingConstants.DefaultMask
        },
        {
            null,
            new SensitiveDataAttribute(),
            null
        },
        {
            string.Empty,
            new SensitiveDataAttribute(),
            string.Empty
        },
    };
}
