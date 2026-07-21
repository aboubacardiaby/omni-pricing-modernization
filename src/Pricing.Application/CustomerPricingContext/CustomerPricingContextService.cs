namespace Pricing.Application.CustomerPricingContext;

using Pricing.Domain.Models;

public sealed class CustomerPricingContextService
{
    private readonly ICustomerPricingContextRepository repository;

    public CustomerPricingContextService(ICustomerPricingContextRepository repository) =>
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>Orchestrates the CUP100 customer-context lookup without applying pricing rules.</summary>
    /// <remarks>
    /// CUP100 A200-SEL-ACCOUNT/A300-PRO-CUST-INFO load identity, A800-A850 load ranked
    /// buying-group/parent and low-UOM facts, 1000 loads exclusions, and 2000/3000 load
    /// fee and component metadata. The repository owns persistence details.
    /// </remarks>
    public async ValueTask<CustomerPricingContextResult> GetAsync(
        PricingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            CustomerPricingContextData? data = await repository.FindAsync(
                request.Division,
                request.Account,
                request.ShipTo,
                request.BillTo,
                request.PricingDate,
                cancellationToken).ConfigureAwait(false);

            if (data is null)
            {
                return CustomerPricingContextResult.Failure(
                    new MissingDataPricingError(
                        "ACCOUNT_NOT_FOUND",
                        "#106 - ACCOUNT NOT FOUND IN CUP100",
                        "106",
                        "account",
                        "2"));
            }

            if (!data.ActiveCustomerFound)
            {
                return CustomerPricingContextResult.Failure(
                    new MissingDataPricingError(
                        "ACTIVE_CUSTOMER_NOT_FOUND",
                        "#108 - ACTIVE CUSTOMER NOT FOUND IN CUP100",
                        "108",
                        "customer",
                        "3"));
            }

            var customer = new CustomerInformation(
                request.Account,
                data.Customer,
                Normalize(data.BuyingGroupMemberships),
                Normalize(data.ContractExclusions),
                Normalize(data.Fees),
                data.LowUnitOfMeasure,
                data.Freight,
                Normalize(data.PriceComponents));
            return CustomerPricingContextResult.Success(customer);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (CustomerPricingContextRepositoryException exception)
        {
            return CustomerPricingContextResult.Failure(
                new DependencyPricingError(
                    "CUSTOMER_CONTEXT_LOOKUP_FAILED",
                    exception.Message,
                    exception.LegacyErrorCode,
                    exception.IsTransient,
                    exception.LegacySeverityCode));
        }
    }

    private static System.Collections.Immutable.ImmutableArray<T> Normalize<T>(
        System.Collections.Immutable.ImmutableArray<T> values) => values.IsDefault ? [] : values;
}
