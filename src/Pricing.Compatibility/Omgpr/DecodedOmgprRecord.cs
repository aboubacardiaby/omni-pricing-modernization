namespace Pricing.Compatibility.Omgpr;

using Pricing.Domain.Models;

public sealed record DecodedOmgprRecord
{
    public DecodedOmgprRecord(
        PricingRequest request,
        OmgprRepresentativeFields representativeFields,
        ReadOnlySpan<byte> originalBytes)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        RepresentativeFields = representativeFields ?? throw new ArgumentNullException(nameof(representativeFields));
        if (originalBytes.Length != OmgprLayout.RecordLength)
        {
            throw new ArgumentException(
                $"Original OMGPR data must be exactly {OmgprLayout.RecordLength} bytes.",
                nameof(originalBytes));
        }

        OriginalBytes = originalBytes.ToArray();
        OpaqueTrailingBytes = OriginalBytes
            .AsSpan(OmgprLayout.OpaqueTrailingOffset, OmgprLayout.OpaqueTrailingLength)
            .ToArray();
    }

    public PricingRequest Request { get; }
    public OmgprRepresentativeFields RepresentativeFields { get; }
    public byte[] OriginalBytes { get; }
    public byte[] OpaqueTrailingBytes { get; }
}

public sealed record OmgprRepresentativeFields(
    long ContractNumber,
    decimal ContractLineUnitCost,
    decimal PricingPercentage,
    long ErrorNumber);
