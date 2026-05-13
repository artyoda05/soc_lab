var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

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
.WithName("GetCustomerByIdLegacy");

app.MapGet("/api/v1/customers/{id:int}", (int id) =>
{
    if (!customers.TryGetValue(id, out var customer))
    {
        return Results.NotFound(new { message = $"Customer {id} was not found." });
    }

    return Results.Ok(customer);
})
.WithName("GetCustomerByIdV1");

app.MapGet("/api/v2/customers/{id:int}", (int id) =>
{
    if (!customers.TryGetValue(id, out var customer))
    {
        return Results.NotFound(new { message = $"Customer {id} was not found." });
    }

    // v2 contract intentionally changed for compatibility demonstration.
    return Results.Ok(new
    {
        customer.Id,
        FullName = customer.Name,
        customer.Email,
        customer.Address
    });
})
.WithName("GetCustomerByIdV2");

app.MapPut("/customers/{id:int}/address", (int id, Address newAddress) =>
{
    if (!customers.TryGetValue(id, out var existing))
    {
        return Results.NotFound(new { message = $"Customer {id} was not found." });
    }

    customers[id] = existing with { Address = newAddress };
    return Results.Ok(customers[id]);
})
.WithName("UpdateCustomerAddress");

app.Run();

record CustomerProfile(int Id, string Name, string Email, Address Address);

record Address(string Country, string City, string PostalCode, string Street);
