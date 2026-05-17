using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapOpenApi();
app.MapScalarApiReference();

var customers = new Dictionary<int, CustomerProfile>
{
    [1] = new(
        Id: 1,
        Name: "Olena Koval",
        Email: "olena.koval@example.com",
        Address: new Address("UA", "Kyiv", "01001", "Khreshchatyk St 1")),
    [2] = new(
        Id: 2,
        Name: "Maksym Bondar",
        Email: "maksym.bondar@example.com",
        Address: new Address("UA", "Lviv", "79000", "Svobody Ave 10"))
};

app.MapGet("/customers/{id:int}", (int id) =>
{
    if (!customers.TryGetValue(id, out var customer))
    {
        return Results.NotFound(new { message = $"Customer {id} was not found." });
    }

    return Results.Ok(customer);
})
.WithName("GetCustomerByIdLegacy")
.WithTags("Customers (Legacy)")
.WithSummary("Get customer by ID (Legacy)")
.WithDescription("Retrieves a customer profile using the original, unversioned endpoint.")
.Produces<CustomerProfile>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);


app.MapGet("/api/v1/customers/{id:int}", (int id) =>
{
    if (!customers.TryGetValue(id, out var customer))
    {
        return Results.NotFound(new { message = $"Customer {id} was not found." });
    }

    return Results.Ok(customer);
})
.WithName("GetCustomerByIdV1")
.WithTags("Customers (V1)")
.WithSummary("Get customer by ID (V1)")
.WithDescription("Retrieves a customer profile using the explicitly versioned v1 contract.")
.Produces<CustomerProfile>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);


app.MapGet("/api/v2/customers/{id:int}", (int id) =>
{
    if (!customers.TryGetValue(id, out var customer))
    {
        return Results.NotFound(new { message = $"Customer {id} was not found." });
    }

    // v2 contract intentionally changed for compatibility demonstration.
    // Mapped to a strongly-typed record to ensure proper OpenAPI schema generation.
    var response = new CustomerProfileV2(
        Id: customer.Id,
        FullName: customer.Name,
        Email: customer.Email,
        Address: customer.Address
    );

    return Results.Ok(response);
})
.WithName("GetCustomerByIdV2")
.WithTags("Customers (V2)")
.WithSummary("Get customer by ID (V2)")
.WithDescription("Retrieves a customer profile. Note: The V2 contract returns 'FullName' instead of 'Name'.")
.Produces<CustomerProfileV2>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);


app.MapPut("/customers/{id:int}/address", (int id, [FromBody] Address newAddress) =>
{
    if (!customers.TryGetValue(id, out var existing))
    {
        return Results.NotFound(new { message = $"Customer {id} was not found." });
    }

    customers[id] = existing with { Address = newAddress };
    return Results.Ok(customers[id]);
})
.WithName("UpdateCustomerAddress")
.WithTags("Customers (Legacy)")
.WithSummary("Update a customer's address")
.WithDescription("Updates the delivery address for an existing customer profile.")
.Produces<CustomerProfile>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();

// Data Contracts
record CustomerProfile(int Id, string Name, string Email, Address Address);

record CustomerProfileV2(int Id, string FullName, string Email, Address Address);

record Address(string Country, string City, string PostalCode, string Street);