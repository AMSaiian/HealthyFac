namespace HealthyFac.Shared.UnitTests.UseCases.Data;

public static class UseCaseRegistrationTestData
{
    public const string BlankName = "";
    public const string AddedTwiceText = "*AddUseCases*once*";

    public static readonly Type[] NoResultTypes =
    [
        typeof(NoResultRequest),
        typeof(NoResultRequestValidator),
        typeof(NoResultRequestPreHandlerCheck),
        typeof(NoResultRequestHandler),
        typeof(HandlerProbe),
    ];

    public static readonly Type[] RuledTypes =
    [
        typeof(RuledRequest),
        typeof(RuledRequestValidator),
        typeof(RuledRequestPreHandlerCheck),
        typeof(RuledRequestHandler),
        typeof(HandlerProbe),
    ];

    public static readonly Type[] UnruledTypes = [typeof(UnruledRequest), typeof(UnruledRequestHandler)];

    public static readonly Type[] DoubleRuledTypes =
    [
        typeof(DoubleRuledRequest),
        typeof(DoubleRuledRequestHandler),
        typeof(FirstDoubleRuledPreHandlerCheck),
        typeof(SecondDoubleRuledPreHandlerCheck),
    ];

    public static readonly Type[] DuplicatedTypes =
    [
        typeof(DuplicatedRequest),
        typeof(FirstDuplicatedRequestHandler),
        typeof(SecondDuplicatedRequestHandler),
    ];
}
