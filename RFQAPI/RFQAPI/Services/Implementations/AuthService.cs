using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Auth;
using RFQAPI.Models.DTOs.Users;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly DapperContext _dapperContext;
    private readonly IUserRepository _userRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly JwtTokenHelper _jwtTokenHelper;

    public AuthService(
        DapperContext dapperContext,
        IUserRepository userRepository,
        ISupplierRepository supplierRepository,
        JwtTokenHelper jwtTokenHelper)
    {
        _dapperContext = dapperContext;
        _userRepository = userRepository;
        _supplierRepository = supplierRepository;
        _jwtTokenHelper = jwtTokenHelper;
    }

    public async Task<RegistrationResponse> RegisterSupplierAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ApiException("A user with this email already exists.", StatusCodes.Status409Conflict);
        }

        if (await _supplierRepository.RegistrationNumberExistsAsync(request.RegistrationNumber, cancellationToken))
        {
            throw new ApiException("A supplier with this registration number already exists.", StatusCodes.Status409Conflict);
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = UserRoles.Supplier,
            IsActive = true
        };

        await using var connection = _dapperContext.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var createdUser = await _userRepository.CreateAsync(user, transaction, cancellationToken);

            var supplier = new Supplier
            {
                UserId = createdUser.UserId,
                CompanyName = request.CompanyName.Trim(),
                RegistrationNumber = request.RegistrationNumber.Trim(),
                ContactPerson = string.IsNullOrWhiteSpace(request.ContactPerson)
                    ? request.FullName.Trim()
                    : request.ContactPerson.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                ApprovalStatus = SupplierApprovalStatuses.Pending
            };

            var createdSupplier = await _supplierRepository.CreateAsync(supplier, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new RegistrationResponse
            {
                User = UserMapper.ToResponse(createdUser),
                Supplier = SupplierMapper.ToResponse(createdSupplier)
            };
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (ex.ConstraintName?.Contains("email", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new ApiException("A user with this email already exists.", StatusCodes.Status409Conflict);
            }

            if (ex.ConstraintName?.Contains("registration", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new ApiException("A supplier with this registration number already exists.", StatusCodes.Status409Conflict);
            }

            throw new ApiException("Registration failed due to a duplicate value.", StatusCodes.Status409Conflict);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ApiException("Invalid email or password.", StatusCodes.Status401Unauthorized);
        }

        if (!user.IsActive)
        {
            throw new ApiException("Account is inactive.", StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAt) = _jwtTokenHelper.CreateToken(user);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = UserMapper.ToResponse(user)
        };
    }
}