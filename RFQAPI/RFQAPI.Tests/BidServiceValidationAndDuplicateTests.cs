using Microsoft.AspNetCore.Http;
using Moq;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Bids;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Implementations;
using Xunit;

namespace RFQAPI.Tests;

/// <summary>
/// Mandatory test 2 (service layer) and additional bid validation rules.
/// </summary>
public class BidServiceValidationAndDuplicateTests
{
    private readonly Mock<IBidRepository> _bidRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();

  private BidService CreateService() =>
        new(_bidRepository.Object, _supplierRepository.Object);

    private void SetupApprovedSupplier(long userId = 10, long supplierId = 5)
    {
        _supplierRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Supplier
            {
                SupplierId = supplierId,
                UserId = userId,
                ApprovalStatus = SupplierApprovalStatuses.Approved,
                CompanyName = "Test Co",
                RegistrationNumber = "REG-TEST",
                ContactPerson = "Tester",
                Phone = "000",
                CreatedAt = DateTimeOffset.UtcNow
            });
    }

    [Fact]
    public async Task SubmitAsync_WhenDuplicateActiveBidInRound_Throws409Conflict()
    {
        SetupApprovedSupplier();
        _bidRepository.Setup(r => r.GetRfqItemIdsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 100 });

        _bidRepository
            .Setup(r => r.SubmitAsync(
                1, 2, 5, 10, It.IsAny<IReadOnlyList<SubmitBidItemRequest>>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException("You already have an active bid for this round.", StatusCodes.Status409Conflict));

        var service = CreateService();
        var request = new SubmitBidRequest
        {
            Items = [new SubmitBidItemRequest { RfqItemId = 100, UnitPrice = 10m }]
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.SubmitAsync(1, 2, 10, request, null, CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, ex.StatusCode);
        Assert.Contains("active bid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_AfterWithdrawal_AllowsSecondSubmitWhenRepositorySucceeds()
    {
        SetupApprovedSupplier();
        _bidRepository.Setup(r => r.GetRfqItemIdsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 100 });

        var firstBid = new Bid { BidId = 1, RfqId = 1, RoundId = 2, SupplierId = 5, Status = BidStatuses.Submitted };
        var secondBid = new Bid { BidId = 2, RfqId = 1, RoundId = 2, SupplierId = 5, Status = BidStatuses.Submitted };

        _bidRepository
            .SetupSequence(r => r.SubmitAsync(
                1, 2, 5, 10, It.IsAny<IReadOnlyList<SubmitBidItemRequest>>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BidDetailData
            {
                Bid = firstBid,
                CurrencyCode = "USD",
                Items = []
            })
            .ReturnsAsync(new BidDetailData
            {
                Bid = secondBid,
                CurrencyCode = "USD",
                Items = []
            });

        _bidRepository
            .Setup(r => r.WithdrawAsync(1, 5, 10, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BidDetailData
            {
                Bid = new Bid { BidId = 1, Status = BidStatuses.Withdrawn, RfqId = 1, RoundId = 2, SupplierId = 5 },
                CurrencyCode = "USD",
                Items = []
            });

        var service = CreateService();
        var request = new SubmitBidRequest
        {
            Items = [new SubmitBidItemRequest { RfqItemId = 100, UnitPrice = 12m }]
        };

        var first = await service.SubmitAsync(1, 2, 10, request, null, CancellationToken.None);
        var withdrawn = await service.WithdrawAsync(1, 10, null, CancellationToken.None);
        var second = await service.SubmitAsync(1, 2, 10, request, null, CancellationToken.None);

        Assert.Equal(BidStatuses.Submitted, first.Status);
        Assert.Equal(BidStatuses.Withdrawn, withdrawn.Status);
        Assert.Equal(2, second.BidId);
        Assert.NotEqual(first.BidId, second.BidId);
    }

    [Fact]
    public async Task SubmitAsync_DifferentSuppliers_CanBothSubmitWhenRepositoryAllows()
    {
        _supplierRepository.Setup(r => r.GetByUserIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Supplier { SupplierId = 5, UserId = 10, ApprovalStatus = SupplierApprovalStatuses.Approved, CompanyName = "A", RegistrationNumber = "R1", ContactPerson = "A", Phone = "1", CreatedAt = DateTimeOffset.UtcNow });
        _supplierRepository.Setup(r => r.GetByUserIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Supplier { SupplierId = 6, UserId = 20, ApprovalStatus = SupplierApprovalStatuses.Approved, CompanyName = "B", RegistrationNumber = "R2", ContactPerson = "B", Phone = "2", CreatedAt = DateTimeOffset.UtcNow });

        _bidRepository.Setup(r => r.GetRfqItemIdsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 100 });

        _bidRepository.Setup(r => r.SubmitAsync(
                1, 2, 5, 10, It.IsAny<IReadOnlyList<SubmitBidItemRequest>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BidDetailData { Bid = new Bid { BidId = 1, SupplierId = 5, RfqId = 1, RoundId = 2, Status = BidStatuses.Submitted }, CurrencyCode = "USD", Items = [] });

        _bidRepository.Setup(r => r.SubmitAsync(
                1, 2, 6, 20, It.IsAny<IReadOnlyList<SubmitBidItemRequest>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BidDetailData { Bid = new Bid { BidId = 2, SupplierId = 6, RfqId = 1, RoundId = 2, Status = BidStatuses.Submitted }, CurrencyCode = "USD", Items = [] });

        var service = CreateService();
        var request = new SubmitBidRequest { Items = [new SubmitBidItemRequest { RfqItemId = 100, UnitPrice = 9m }] };

        var a = await service.SubmitAsync(1, 2, 10, request, null, CancellationToken.None);
        var b = await service.SubmitAsync(1, 2, 20, request, null, CancellationToken.None);

        Assert.Equal(1, a.BidId);
        Assert.Equal(2, b.BidId);
    }

    [Fact]
    public async Task SubmitAsync_WhenUnitPriceNotPositive_Throws400()
    {
        SetupApprovedSupplier();
        _bidRepository.Setup(r => r.GetRfqItemIdsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 100, 101 });

        var service = CreateService();
        var request = new SubmitBidRequest
        {
            Items =
            [
                new SubmitBidItemRequest { RfqItemId = 100, UnitPrice = 0m },
                new SubmitBidItemRequest { RfqItemId = 101, UnitPrice = 5m }
            ]
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.SubmitAsync(1, 2, 10, request, null, CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        _bidRepository.Verify(r => r.SubmitAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(),
            It.IsAny<IReadOnlyList<SubmitBidItemRequest>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_WhenNotAllRfqItemsQuoted_Throws400()
    {
        SetupApprovedSupplier();
        _bidRepository.Setup(r => r.GetRfqItemIdsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { 100, 101 });

        var service = CreateService();
        var request = new SubmitBidRequest
        {
            Items = [new SubmitBidItemRequest { RfqItemId = 100, UnitPrice = 5m }]
        };

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.SubmitAsync(1, 2, 10, request, null, CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        Assert.Contains("All RFQ items", ex.Message, StringComparison.Ordinal);
    }
}