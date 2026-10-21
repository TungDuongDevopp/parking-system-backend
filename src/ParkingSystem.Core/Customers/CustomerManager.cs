using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization.Users;
using ParkingSystem.Entities;
using ParkingSystem.Exceptions;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParkingSystem.Customers;

public class CustomerManager : DomainService
{
    private readonly IRepository<Customer, long> _customerRepository;

    public CustomerManager(IRepository<Customer, long> customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> CreateProfileForUserAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        // 1. Ensure user does not already have a customer profile
        var existingCustomer = await _customerRepository
            .GetAll()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (existingCustomer != null)
        {
            throw new DuplicateResourceException("Current user already has a customer profile.");
        }

        // 2. Format and validate Name (Surname + " " + Name)
        var rawName = $"{user.Surname} {user.Name}";
        var normalizedName = Regex.Replace(rawName.Trim(), @"\s+", " ");

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new BusinessRuleException("Customer name cannot be empty.");
        }

        if (normalizedName.Length > 150)
        {
            throw new BusinessRuleException("Customer name exceeds maximum allowed length of 150 characters.");
        }

        // 3. Format and validate Email (Required, max 256)
        if (string.IsNullOrWhiteSpace(user.EmailAddress))
        {
            throw new BusinessRuleException("Customer email cannot be empty.");
        }

        var email = user.EmailAddress.Trim();
        if (email.Length > 256)
        {
            throw new BusinessRuleException("Customer email exceeds maximum allowed length of 256 characters.");
        }

        var emailExists = await _customerRepository
            .GetAll()
            .AnyAsync(x => x.Email == email);

        if (emailExists)
        {
            throw new DuplicateResourceException("A customer with the same email already exists.");
        }

        // 4. Construct Customer entity with null PhoneNumber
        var customer = new Customer
        {
            UserId = user.Id,
            Name = normalizedName,
            Email = email,
            PhoneNumber = null
        };

        return await _customerRepository.InsertAsync(customer);
    }
}
