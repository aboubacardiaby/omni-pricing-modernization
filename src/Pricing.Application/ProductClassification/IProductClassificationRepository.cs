namespace Pricing.Application.ProductClassification;

using Pricing.Domain.ValueObjects;

public interface IProductClassificationRepository
{
    /// <summary>Reads VNG02.C_PRODUCT_TYPE for the vendor/product key.</summary>
    /// <remarks>COBOL: A6O016U, 2200-READ-PRODUCT-DATA and 9015-SELECT-VNG02.</remarks>
    ValueTask<string?> FindLegacyProductTypeAsync(
        VendorId vendor,
        ProductId product,
        CancellationToken cancellationToken);
}

public sealed class ProductClassificationRepositoryException : Exception
{
    public ProductClassificationRepositoryException(string message, bool isTransient = false, Exception? innerException = null)
        : base(message, innerException) => IsTransient = isTransient;

    public bool IsTransient { get; }
}
