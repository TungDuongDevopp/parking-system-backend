using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Timing;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ParkingSystem.PaymentTransactions;

public class TransactionManager : DomainService
{
    private readonly IRepository<PaymentTransaction, long> _repository;

    public TransactionManager(IRepository<PaymentTransaction, long> repository)
    {
        _repository = repository;
    }

    private static string GenerateTransactionCode()
    {
        return $"Trans-{Convert.ToHexString(RandomNumberGenerator.GetBytes(12))}";
    }

    /// <summary>
    /// Tao giao dich thanh toan dau tien cho mot Payment.
    /// Phai duoc goi trong cung UoW voi viec tao Payment de dam bao atomicity.
    /// </summary>
    public async Task<PaymentTransaction> CreatePaymentTransactionAsync(
     Payment payment)
    {
        var dbContext = _repository.GetDbContext();

        var transactionCode = GenerateTransactionCode();

        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        INSERT INTO PaymentTransactions
            (PaymentId, Type, TransactionCode, Amount, Status, CreationTime)
        SELECT
            p.Id,
            {(int)TransactionType.Payment},
            {transactionCode},
            p.Amount,
            {(int)TransactionStatus.Pending},
            GETUTCDATE()
        FROM Payments AS p
        WHERE p.Id = {payment.Id}
          AND p.PaymentStatus = {(int)PaymentStatus.Pending}
          AND p.ExpiresAt > GETUTCDATE();
    ");

        if (rowsAffected == 0)
            throw new BusinessRuleException(
                "Payment khong con hieu luc hoac da het han hoac da tra xong.");

        var entity = await _repository.FirstOrDefaultAsync(
            x => x.TransactionCode == transactionCode);

        if (entity == null)
            throw new BusinessRuleException(
                "Khong the lay Transaction vua tao.");

        return entity;
    }

    /// <summary>
    /// Tao giao dich hoan tien. Khong sua Transaction cu.
    /// Chi cho phep hoan tien khi Payment da Paid.
    /// </summary>
    public async Task<PaymentTransaction> CreateRefundTransactionAsync(Payment payment)
    {
        var dbContext = _repository.GetDbContext();

        var transactionCode = GenerateTransactionCode();

        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        INSERT INTO PaymentTransactions
            (PaymentId, Type, TransactionCode, Amount, Status, CreationTime)
        SELECT
            p.Id,
            {(int)TransactionType.Payment},
            {transactionCode},
            p.Amount,
            {(int)TransactionStatus.Pending},
            GETUTCDATE()
        FROM Payments AS p
        WHERE p.Id = {payment.Id}
          AND p.PaymentStatus = {(int)PaymentStatus.Paid}
          AND p.ExpiresAt > GETUTCDATE()
          AND p.Amount > 0;
    ");

        if (rowsAffected == 0)
            throw new BusinessRuleException(
                "Payment khong con hieu luc hoac da het han hoac da tra xong.");

        var entity = await _repository.FirstOrDefaultAsync(
            x => x.TransactionCode == transactionCode);

        if (entity == null)
            throw new BusinessRuleException(
                "Khong the lay Transaction vua tao.");

        return entity;
    }

    /// <summary>
    /// Danh dau Transaction that bai.
    /// Chi Transaction dang Pending moi duoc chuyen Failed.
    /// Payment van giu nguyen Pending neu con han - khong tu dong chuyen Payment thanh Failed.
    /// </summary>
    public async Task FailedAsync(PaymentTransaction transaction)
    {
        var dbContext = _repository.GetDbContext();

        var rowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE PaymentTransactions
        SET    [Status] = {(int)TransactionStatus.Failed},
               TransactionTime = GETUTCDATE(),
               LastModificationTime = GETUTCDATE()
        WHERE  Id = {transaction.Id}
          AND  [Status] = {(int)TransactionStatus.Pending}
        ");

        if (rowsAffected == 0)
            throw new BusinessRuleException(
                "Transaction khong hop le de xu ly that bai: khong ton tai hoac khong o trang thai Pending.");
    }

    /// <summary>
    /// Hoan tat Transaction thanh cong va chuyen Payment sang Paid.
    /// Dung conditional UPDATE de dam bao khong co race condition.
    /// Hai cau UPDATE nam trong cung DB connection/transaction cua EF Core.
    /// </summary>
    public async Task CompleteAsync(PaymentTransaction transaction)
    {
        var dbContext = _repository.GetDbContext();

        var txRowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE pt
        SET    pt.[Status] = {(int)TransactionStatus.Success},
               pt.TransactionTime = GETUTCDATE(),
               pt.LastModificationTime = GETUTCDATE()
        FROM   PaymentTransactions AS pt
        INNER JOIN Payments AS p ON p.Id = pt.PaymentId
        WHERE  pt.Id = {transaction.Id}
          AND  pt.[Status] = {(int)TransactionStatus.Pending}
          AND pt.Amount > 0
          AND  p.PaymentStatus = {(int)PaymentStatus.Pending}
          AND  p.ExpiresAt > GETUTCDATE()
        ");

        if (txRowsAffected == 0)
            throw new BusinessRuleException(
                "Transaction hoac Payment khong hop le de hoan tat: " +
                "da xu ly, Payment khong con Pending, het han hoac Amount = 0.");

        var paymentRowsAffected = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE p
        SET    p.PaymentStatus = {(int)PaymentStatus.Paid},
               p.PaidAt = GETUTCDATE(),
               p.LastModificationTime = GETUTCDATE()
        FROM   Payments AS p
        INNER JOIN PaymentTransactions AS pt ON pt.PaymentId = p.Id
        WHERE  pt.Id = {transaction.Id}
          AND  p.PaymentStatus = {(int)PaymentStatus.Pending}
        ");

        if (paymentRowsAffected == 0)
            throw new BusinessRuleException(
                "Khong the cap nhat Payment sang Paid: co the da duoc thanh toan boi request khac (race condition).");
    }
}