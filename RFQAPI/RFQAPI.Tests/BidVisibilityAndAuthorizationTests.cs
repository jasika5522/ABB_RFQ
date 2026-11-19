using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using RFQAPI.Controllers;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Implementations;
using Xunit;

namespace RFQAPI.Tests;

/// <summary>
/// Mandatory test 3: role-based bid visibility and HTTP authorization.
/// </summary>
public class BidVisibilityAndAuthorizationTests
{
    private const string TestJwtKey = "UnitTestSigningKey_MustBeAtLeast32Chars!";

    [Fact]
    public async Task GetMyBidDetailAsync_WhenBidBelongsToAnotherSupplier_Returns404NotFound()
    {
        var bidRepo = new Mock<IBidRepository>();
        var supplierRepo = new Mock<ISupplierRepository>();

        supplierRepo.Setup(r => r.GetByUserIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Supplier
            {
                SupplierId = 5,
                UserId = 10,
                ApprovalStatus = SupplierApprovalStatuses.Approved,
                CompanyName = "Supplier A",
                RegistrationNumber = "A-1",
                ContactPerson = "A",
                Phone = "1",
                CreatedAt = DateTimeOffset.UtcNow
            });

        bidRepo.Setup(r => r.GetDetailForSupplierAsync(99, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BidDetailData?)null);

        var service = new BidService(bidRepo.Object, supplierRepo.Object);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetMyBidDetailAsync(99, 10, CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
        bidRepo.Verify(r => r.GetDetailForSupplierAsync(99, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMyBids_WithoutAuthentication_Returns401()
    {
        await using var factory = new RfqApiWebApplicationFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/bids/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyBids_WithProcurementOfficerRole_Returns403()
    {
        await using var factory = new RfqApiWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(UserRoles.ProcurementOfficer, userId: 2));

        var response = await client.GetAsync("/api/bids/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SubmitBid_WithProcurementOfficerRole_Returns403()
    {
        await using var factory = new RfqApiWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(UserRoles.ProcurementOfficer, userId: 2));

        var content = new StringContent("{\"items\":[]}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/rfqs/1/rounds/1/bids", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEvaluation_WithSupplierRole_Returns403()
    {
        await using var factory = new RfqApiWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(UserRoles.Supplier, userId: 3));

        var response = await client.GetAsync("/api/rfqs/1/rounds/1/evaluation");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

  [Fact]
    public void BidEndpoints_AreRestrictedToSupplierRoleOnly()
    {
        var authorize = typeof(BidsController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal(UserRoles.Supplier, authorize!.Roles);
    }

    private static string CreateToken(string role, long userId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Email, $"{role}@test.local")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "RFQAPI",
            audience: "RFQAPI",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class RfqApiWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Jwt:Key", TestJwtKey);
            builder.UseSetting("Jwt:Issuer", "RFQAPI");
            builder.UseSetting("Jwt:Audience", "RFQAPI");
            builder.UseSetting("Jwt:ExpirationMinutes", "60");
            builder.UseSetting(
                "ConnectionStrings:RfqDb",
                "Host=127.0.0.1;Port=54329;Database=rfq_tests_unused;Username=test;Password=test;");
        }
    }
}