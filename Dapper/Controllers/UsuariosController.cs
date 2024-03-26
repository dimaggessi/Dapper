using eCommerce.Models;
using eCommerce.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Controllers;

[ApiController]
[Route("[controller]")]
public class UsuariosController : Controller
{
    private readonly IUsuarioRepository _repository;
    public UsuariosController(IUsuarioRepository usuarioRepository)
    {
        _repository = usuarioRepository;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var result = _repository.Get();

        return Ok(result);
    }

    [HttpGet("id")]
    public IActionResult Get([FromQuery] int id)
    {
        var result = _repository.Get(id);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public IActionResult Post([FromBody] Usuario usuario)
    {
        try
        {
            usuario.DataCadastro = DateTimeOffset.Now;
            usuario.SituacaoCadastro = "A";

            _repository.Create(usuario);
        }
        catch (Exception)
        {
            return StatusCode(500, "Erro ao tentar inserir usuário");
        }
        return Ok(usuario);
    }

    [HttpPut]
    public IActionResult Put([FromBody] Usuario usuario)
    {
        _repository.Update(usuario);
        return Ok(usuario);
    }

    [HttpDelete("id")]
    public IActionResult Delete([FromQuery] int id)
    {
        _repository.Delete(id);

        return Ok();
    }
}
