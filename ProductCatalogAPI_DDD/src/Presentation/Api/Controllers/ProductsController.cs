using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductCatalogAPI.Application.Features.Products.Commands;
using ProductCatalogAPI.Application.Features.Products.Queries;

namespace ProductCatalogAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Controller hanya meneruskan request ke MediatR (Thin Controller).
        var result = await _mediator.Send(new GetProductsQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // Query sederhana tapi tetap melalui MediatR untuk konsistensi.
        // (Catatan: Handler GetProductById belum dibuat, bisa ditambahkan jika perlu).
        return Ok(); 
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateProductCommand command)
    {
        // Logika bisnis sudah ada di Application/Domain layer.
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UpdateProductCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteProductCommand(id));
        if (!result) return NotFound();

        return NoContent();
    }
}
