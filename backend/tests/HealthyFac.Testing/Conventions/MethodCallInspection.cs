using System.Reflection;
using System.Reflection.Emit;

namespace HealthyFac.Testing.Conventions;

/// <summary>
/// Finds the calls that the methods of a type make, by reading the IL of each method body. Reflection alone shows no
/// call, so a rule such as "only one class begins a transaction" needs the IL.
/// </summary>
internal static class MethodCallInspection
{
    private const byte TwoByteOpCodePrefix = 0xFE;
    private const int ByteSize = 1;
    private const int ShortSize = 2;
    private const int IntSize = 4;
    private const int LongSize = 8;

    private static readonly Lazy<OpCodeTable> OpCodes = new(OpCodeTable.Build);

    /// <summary>
    /// Finds the calls to a target method in every method and constructor that the type declares, and in every type
    /// nested in it, at any depth. The nested types hold the async state machines, the closures of lambdas, and the
    /// local functions, so a call in a lambda counts for the type that declares the lambda.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <param name="isTarget">Tells whether a called method is a target.</param>
    /// <returns>The pairs of the calling method and the called target.</returns>
    public static IEnumerable<(MethodBase Caller, MethodBase Callee)> FindCalls(
        Type type,
        Func<MethodBase, bool> isTarget
    ) =>
        EnumerateTypes(type)
            .SelectMany(declared => declared.GetMethods(TypeInspection.DeclaredMembers)
                .Cast<MethodBase>()
                .Concat(declared.GetConstructors(TypeInspection.DeclaredMembers)))
            .SelectMany(caller => ReadCallees(caller)
                .Where(isTarget)
                .Select(callee => (caller, callee)));

    private static IEnumerable<Type> EnumerateTypes(Type type)
    {
        yield return type;

        foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(EnumerateTypes))
        {
            yield return nested;
        }
    }

    private static IEnumerable<MethodBase> ReadCallees(MethodBase caller)
    {
        var il = caller.GetMethodBody()?.GetILAsByteArray();

        if (il is null)
        {
            yield break;
        }

        var typeArguments = caller.DeclaringType is { IsGenericType: true } declaring
            ? declaring.GetGenericArguments()
            : null;

        var methodArguments = caller.IsGenericMethod ? caller.GetGenericArguments() : null;
        var position = 0;

        while (position < il.Length)
        {
            var opCode = OpCodes.Value.Read(il, ref position);

            if (opCode.OperandType == OperandType.InlineMethod)
            {
                var token = BitConverter.ToInt32(il, position);
                var callee = TryResolve(caller.Module, token, typeArguments, methodArguments);

                if (callee is not null)
                {
                    yield return callee;
                }
            }

            position += GetOperandSize(opCode, il, position);
        }
    }

    private static MethodBase? TryResolve(Module module, int token, Type[]? typeArguments, Type[]? methodArguments)
    {
        try
        {
            return module.ResolveMethod(token, typeArguments, methodArguments);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static int GetOperandSize(OpCode opCode, byte[] il, int position) => opCode.OperandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => ByteSize,
        OperandType.InlineVar => ShortSize,
        OperandType.InlineBrTarget
            or OperandType.InlineField
            or OperandType.InlineI
            or OperandType.InlineMethod
            or OperandType.InlineSig
            or OperandType.InlineString
            or OperandType.InlineTok
            or OperandType.InlineType
            or OperandType.ShortInlineR => IntSize,
        OperandType.InlineI8 or OperandType.InlineR => LongSize,
        OperandType.InlineSwitch => IntSize + IntSize * BitConverter.ToInt32(il, position),
        _ => throw new NotSupportedException($"The operand type {opCode.OperandType} is not supported."),
    };

    private sealed class OpCodeTable
    {
        private readonly Dictionary<byte, OpCode> _oneByte = [];
        private readonly Dictionary<byte, OpCode> _twoByte = [];

        public static OpCodeTable Build()
        {
            var table = new OpCodeTable();

            var opCodes = typeof(OpCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(OpCode))
                .Select(field => (OpCode)field.GetValue(obj: null)!);

            foreach (var opCode in opCodes)
            {
                var isTwoByte = opCode.Size == ShortSize;
                (isTwoByte ? table._twoByte : table._oneByte)[(byte)(opCode.Value & 0xFF)] = opCode;
            }

            return table;
        }

        public OpCode Read(byte[] il, ref int position)
        {
            var first = il[position++];

            return first == TwoByteOpCodePrefix ? _twoByte[il[position++]] : _oneByte[first];
        }
    }
}
