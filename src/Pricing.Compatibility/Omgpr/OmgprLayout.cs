namespace Pricing.Compatibility.Omgpr;

internal static class OmgprLayout
{
    internal const int RecordLength = 1789;
    internal const int MappedLength = 1773;
    internal const int OpaqueTrailingOffset = 1773;
    internal const int OpaqueTrailingLength = 16;

    internal static class Input
    {
        internal static readonly Field Division = new(0, 2);
        internal static readonly Field Account = new(2, 6);
        internal static readonly Field Vendor = new(8, 4);
        internal static readonly Field Product = new(12, 8);
        internal const int QuantityOffset = 20;
        internal static readonly Field UnitOfMeasure = new(24, 2);
        internal static readonly Field ShipToSuffix = new(26, 3);
        internal static readonly Field BillToSuffix = new(29, 3);
        internal static readonly Field PricingDate = new(32, 10);
        internal static readonly Field PricingRequestSwitch = new(140, 1);
        internal static readonly Field SpecialContractFlag = new(151, 1);

        internal static Field Quantity(int byteLength) => new(QuantityOffset, byteLength);
    }

    internal static class Output
    {
        internal static readonly Field PricerErrorFlag = new(308, 1);
        internal static readonly Field ErrorMessage = new(309, 76);
        internal static readonly Field SellPrice = new(909, 7);
        internal static readonly Field TotalCost = new(916, 7);
        internal static readonly Field ExpirationDate = new(1254, 10);
        internal static readonly Field ContractNumber = new(523, 4);
        internal static readonly Field ContractLineUnitCost = new(775, 7);
        internal static readonly Field PricingPercentage = new(968, 3);
        internal static readonly Field ErrorNumber = new(1305, 2);
        internal static readonly Field ErrorSeverityCode = new(1307, 3);
    }

    internal readonly record struct Field(int Offset, int Length)
    {
        internal ReadOnlySpan<byte> Read(ReadOnlySpan<byte> record) => record.Slice(Offset, Length);
        internal Span<byte> Write(Span<byte> record) => record.Slice(Offset, Length);
    }
}
