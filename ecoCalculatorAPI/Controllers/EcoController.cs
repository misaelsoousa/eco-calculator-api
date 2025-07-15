using EcoCalculator.Communication.Requests;
using ecoCalculatorAPI.Entities;
using ecoCalculatorAPI.UseCases;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ecoCalculatorAPI.Controllers;
[Route("api/[controller]")]
[ApiController]
public class EcoController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] Co2Request request)
    {
        var useCase = new Co2Calculation();
        var relatorio = await useCase.CalcularEmissoes(request);
        return Ok(relatorio);
    }
    
    [HttpGet("municipios")]
    public async Task<IActionResult> GetCity()
    {
        var useCase = new Co2Calculation();
        var cities = await useCase.GetCities();
        return Ok(cities);
    }


   
}
