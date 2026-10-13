using CajaApp.Data;
using CajaApp.DTOs;
using CajaApp.Helpers;
using CajaApp.Models;
using Microsoft.EntityFrameworkCore;
using CajaApp.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CajaApp.Services;

/// <summary>
/// Validación de subsidio contra el Gateway de Integración (RF-CAJ-06).
/// Se usa cuando ApiSettings:UseMockSubsidio es false.
/// GET {BaseUrl}/pacientes/{cedula}/subsidio
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISessionService _sessionService;
    private readonly string _baseUrl;

    public ApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ISessionService sessionService)
    {
        _httpClient = httpClient;
        _sessionService = sessionService;
        _baseUrl = (configuration["ApiSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
    }

    public async Task<SubsidioResponseDto> ValidarSubsidioAsync(string cedula)
    {
        if (!CedulaHelper.EsValida(cedula))
        {
            return new SubsidioResponseDto
            {
                Encontrado = false,
                Mensaje = "La cédula debe tener 11 dígitos (formato 000-0000000-0)."
            };
        }

        string cedulaNormalizada = CedulaHelper.Normalizar(cedula);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_baseUrl}/pacientes/{Uri.EscapeDataString(cedulaNormalizada)}/subsidio");

        if (!string.IsNullOrWhiteSpace(_sessionService.Token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _sessionService.Token);
        }

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return await ValidarSinConexionAsync(cedulaNormalizada);
        }

        using var respuestaHttp = response;

        if ((int)response.StatusCode >= 500)
        {
            return await ValidarSinConexionAsync(cedulaNormalizada);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new SubsidioResponseDto
            {
                Encontrado = false,
                Mensaje = "Cédula no encontrada en el sistema."
            };
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"el servidor respondió {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var resultado = await response.Content.ReadFromJsonAsync<SubsidioResponseDto>()
            ?? throw new InvalidOperationException("el servidor devolvió una respuesta vacía.");

        if (resultado.Encontrado)
        {
            await GuardarEnTerminalAsync(cedulaNormalizada, resultado);
        }

        return resultado;
    }


    // ==========================================
    // OPERACIÓN CONTINUA
    // ==========================================

    /// <summary>
    /// Guarda el último resultado conocido (sin datos clínicos) para poder
    /// validar si luego el servidor no responde.
    /// </summary>
    private static async Task GuardarEnTerminalAsync(string cedula, SubsidioResponseDto r)
    {
        try
        {
            using var db = new AppDbContext();

            var paciente = await db.Pacientes.FirstOrDefaultAsync(p => p.Cedula == cedula);

            if (paciente is null)
            {
                paciente = new Paciente { Cedula = cedula, NombreMinimo = "Paciente CORE" };
                db.Pacientes.Add(paciente);
            }

            paciente.TieneSubsidio = r.TieneSubsidio && r.Vigente;
            paciente.PorcentajeSubsidio = r.Porcentaje;
            paciente.VigenciaHasta = r.VigenciaHasta;

            await db.SaveChangesAsync();
        }
        catch
        {
            // La caché es una ayuda: si falla, la validación en línea sigue valiendo.
        }
    }

    private static async Task<SubsidioResponseDto> ValidarSinConexionAsync(string cedula)
    {
        var local = await new MockApiService().ValidarSubsidioAsync(cedula);

        local.SinConexion = true;

        local.Mensaje = local.Encontrado
            ? $"{local.Mensaje} (validado sin conexión con el último dato guardado en esta terminal)"
            : "Sin conexión con el servidor y la cédula no está guardada en esta terminal.";

        return local;
    }
}
