using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Json;
using Abp.Web.Models;
using ParkingSystem.Customers.Dto;
using ParkingSystem.Entities;
using ParkingSystem.Models.TokenAuth;
using Shouldly;
using Xunit;

namespace ParkingSystem.Web.Tests.Controllers
{
    public class ExceptionHandling_Tests : ParkingSystemWebTestBase
    {
        private async Task AuthenticateAsAdminAsync()
        {
            await AuthenticateAsync(null, new AuthenticateModel
            {
                UserNameOrEmailAddress = "admin",
                Password = "123qwe"
            });
        }

        [Fact]
        public async Task Delete_NonExistent_Customer_Should_Return_404_With_Specific_Message()
        {
            // Arrange
            await AuthenticateAsAdminAsync();
            const long nonExistentId = 999999;

            // Act
            var response = await Client.DeleteAsync($"/api/services/app/Customer/Delete?Id={nonExistentId}");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

            var json = await response.Content.ReadAsStringAsync();
            var ajaxResponse = JsonSerializer.Deserialize<AjaxResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ajaxResponse.ShouldNotBeNull();
            ajaxResponse.Success.ShouldBeFalse();
            ajaxResponse.Error.ShouldNotBeNull();
            ajaxResponse.Error.Message.ShouldBe($"Customer not found with id: {nonExistentId}");
        }

        [Fact]
        public async Task Create_Duplicate_Customer_Should_Return_409_With_Specific_Message()
        {
            // Arrange
            await AuthenticateAsAdminAsync();
            const string duplicatePhone = "0988888888";

            // Seed existing customer in DB
            UsingDbContext(context =>
            {
                context.Customers.Add(new Customer
                {
                    Name = "Existing Customer",
                    PhoneNumber = duplicatePhone,
                    Email = "existing@example.com"
                });
            });

            var createDto = new CreateCustomerDto
            {
                Name = "New Duplicate Customer",
                PhoneNumber = duplicatePhone,
                Email = "another@example.com"
            };

            // Act
            var response = await Client.PostAsync(
                "/api/services/app/Customer/Create",
                new StringContent(createDto.ToJsonString(), Encoding.UTF8, "application/json")
            );

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

            var json = await response.Content.ReadAsStringAsync();
            var ajaxResponse = JsonSerializer.Deserialize<AjaxResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ajaxResponse.ShouldNotBeNull();
            ajaxResponse.Success.ShouldBeFalse();
            ajaxResponse.Error.ShouldNotBeNull();
            ajaxResponse.Error.Message.ShouldContain("already exists");
        }

        [Fact]
        public async Task Manipulate_SoftDeleted_Customer_Should_Return_404_With_Specific_Message()
        {
            // Arrange
            await AuthenticateAsAdminAsync();
            long deletedCustomerId = 0;

            UsingDbContext(context =>
            {
                var customer = new Customer
                {
                    Name = "Deleted Customer",
                    PhoneNumber = "0977777777",
                    Email = "deleted@example.com",
                    IsDeleted = true
                };
                context.Customers.Add(customer);
                context.SaveChanges();
                deletedCustomerId = customer.Id;
            });

            // Act: attempt to delete the soft-deleted customer
            var response = await Client.DeleteAsync($"/api/services/app/Customer/Delete?Id={deletedCustomerId}");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

            var json = await response.Content.ReadAsStringAsync();
            var ajaxResponse = JsonSerializer.Deserialize<AjaxResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ajaxResponse.ShouldNotBeNull();
            ajaxResponse.Success.ShouldBeFalse();
            ajaxResponse.Error.ShouldNotBeNull();
            ajaxResponse.Error.Message.ShouldBe($"Customer not found with id: {deletedCustomerId}");
        }

        [Fact]
        public async Task Unauthenticated_Customer_Request_Should_Return_401_Or_403()
        {
            // Arrange: Ensure no Authorization header is present
            Client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await Client.DeleteAsync("/api/services/app/Customer/Delete?Id=1");

            // Assert: ABP's default authorization mechanism must block with 401 or 403
            var isUnauthorizedOrForbidden = response.StatusCode == HttpStatusCode.Unauthorized ||
                                            response.StatusCode == HttpStatusCode.Forbidden;
            isUnauthorizedOrForbidden.ShouldBeTrue($"Expected 401 or 403, but got {response.StatusCode}");
        }

        [Fact]
        public async Task Unexpected_Exception_Should_Return_500_Without_Leaking_Internal_Details()
        {
            // Act: trigger an unexpected system exception in test endpoint
            var response = await Client.GetAsync("/api/ExceptionTest/ThrowUnexpectedException");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

            var json = await response.Content.ReadAsStringAsync();
            var ajaxResponse = JsonSerializer.Deserialize<AjaxResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ajaxResponse.ShouldNotBeNull();
            ajaxResponse.Success.ShouldBeFalse();
            ajaxResponse.Error.ShouldNotBeNull();

            // Must NOT leak the sensitive exception message or internal details to the client
            ajaxResponse.Error.Message.ShouldNotContain("Sensitive internal database connection string failed");
            json.ShouldNotContain("Sensitive internal database connection string failed");
            json.ShouldNotContain("InvalidOperationException");
        }
    }
}
