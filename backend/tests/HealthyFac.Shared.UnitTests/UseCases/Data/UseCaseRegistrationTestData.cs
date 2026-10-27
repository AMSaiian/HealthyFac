namespace HealthyFac.Shared.UnitTests.UseCases.Data;

public static class UseCaseRegistrationTestData
{
    public const string BlankName = "";
    public const string AddedTwiceText = "*AddUseCases*once*";

    public static readonly Type[] NoResultTypes =
    [
        typeof(NoResultRequest),
        typeof(NoResultRequestValidator),
        typeof(NoResultRequestHandler),
        typeof(HandlerProbe),
    ];

    public static readonly Type[] DuplicatedTypes =
    [
        typeof(DuplicatedRequest),
        typeof(FirstDuplicatedRequestHandler),
        typeof(SecondDuplicatedRequestHandler),
    ];
}
