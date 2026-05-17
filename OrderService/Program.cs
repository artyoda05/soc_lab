using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var customerServiceBaseUrl = builder.Configuration["CustomerService:BaseUrl"]
    ?? "http://localhost:5231";

builder.Services.AddHttpClient("CustomerService", client =>
{
    client.BaseAddress = new Uri(customerServiceBaseUrl);
});

var app = builder.Build();

app.UseHttpsRedirection();

app.MapOpenApi();
app.MapScalarApiReference();

var orders = new Dictionary<int, Order>();
var nextOrderId = 1;

app.MapPost("/orders", async Task<IResult> (
    CreateOrderRequest request,
    [FromQuery] string? customerApiVersion,
    IHttpClientFactory httpClientFactory) =>
{
    var customerClient = httpClientFactory.CreateClient("CustomerService");
    var version = string.IsNullOrWhiteSpace(customerApiVersion)
        ? "v1"
        : customerApiVersion.Trim().ToLowerInvariant();
    var customerPath = version switch
    {
        "v2" => $"/api/v2/customers/{request.CustomerId}",
        _ => $"/api/v1/customers/{request.CustomerId}"
    };

    var response = await customerClient.GetAsync(customerPath);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Json(new
        {
            message = "Order-Service could not validate customer data.",
            customerServiceStatus = (int)response.StatusCode
        }, statusCode: StatusCodes.Status502BadGateway);
    }

    var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
    if (customer is null || string.IsNullOrWhiteSpace(customer.Name))
    {
        return Results.Json(new
        {
            message = "Contract violation: expected field 'name' is missing in Customer-Service response."
        }, statusCode: StatusCodes.Status502BadGateway);
    }

    var orderId = nextOrderId++;
    var newOrder = new Order(
        Id: orderId,
        CustomerId: request.CustomerId,
        Items: request.Items,
        Status: "Finalized",
        ShippingCity: customer.Address.City,
        CreatedAtUtc: DateTime.UtcNow);

    orders[orderId] = newOrder;

    return Results.Created($"/orders/{orderId}", newOrder);
})
.WithName("CreateOrder")
.WithTags("Orders")
.WithSummary("Create a new order")
.WithDescription("Creates a new order by first validating the customer details against the Customer Service.")
.Produces<Order>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status502BadGateway);


app.MapGet("/orders/{id:int}", (int id) =>
{
    if (!orders.TryGetValue(id, out var order))
    {
        return Results.NotFound(new { message = $"Order {id} was not found." });
    }

    return Results.Ok(order);
})
.WithName("GetOrderById")
.WithTags("Orders")
.WithSummary("Get an order by ID")
.WithDescription("Retrieves a finalized order from the system using its unique identifier.")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();


// Data Contracts
record CreateOrderRequest(int CustomerId, List<OrderItem> Items);
record OrderItem(string Sku, int Quantity);
record Order(
    int Id,
    int CustomerId,
    List<OrderItem> Items,
    string Status,
    string ShippingCity,
    DateTime CreatedAtUtc);

class CustomerDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public CustomerAddressDto Address { get; set; } = new();
}

class CustomerAddressDto
{
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
}