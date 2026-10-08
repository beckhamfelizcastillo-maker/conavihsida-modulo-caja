using CajaApp.DTOs.Sync;
using CajaApp.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class SyncApiService : ISyncApiService
{
    private readonly HttpClient _httpClient;

    private readonly string _baseUrl;

    private readonly ISessionService _sessionService;


    public SyncApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ISessionService sessionService)
    {
        _httpClient = httpClient;

        _sessionService = sessionService;

        _baseUrl =
            configuration["ApiSettings:BaseUrl"]
            ?? string.Empty;
    }


    // ==========================================
    // VERIFICAR DISPONIBILIDAD
    // ==========================================

    public async Task<bool> EstaDisponibleAsync()
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return false;
        }

        try
        {
            using var response =
                await _httpClient.GetAsync(
                    $"{_baseUrl}/health");

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }


    // ==========================================
    // TURNO
    // ==========================================

    public async Task<SyncResponseDto>
        SincronizarTurnoAsync(
            SyncTurnoCajaDto turno)
    {
        return await EnviarAsync(
            "/sync/turnos",
            turno);
    }


    // ==========================================
    // MOVIMIENTO
    // ==========================================

    public async Task<SyncResponseDto>
        SincronizarMovimientoAsync(
            SyncMovimientoCajaDto movimiento)
    {
        return await EnviarAsync(
            "/sync/movimientos",
            movimiento);
    }


    // ==========================================
    // TRANSACCIÓN
    // ==========================================

    public async Task<SyncResponseDto>
        SincronizarTransaccionAsync(
            SyncTransaccionDto transaccion)
    {
        return await EnviarAsync(
            "/sync/transacciones",
            transaccion);
    }


    // ==========================================
    // COMPROBANTE
    // ==========================================

    public async Task<SyncResponseDto>
        SincronizarComprobanteAsync(
            SyncComprobanteDto comprobante)
    {
        return await EnviarAsync(
            "/sync/comprobantes",
            comprobante);
    }


    // ==========================================
    // FONDO
    // ==========================================

    public async Task<SyncResponseDto>
        SincronizarFondoAsync(
            SyncFondoConvenioDto fondo)
    {
        return await EnviarAsync(
            "/sync/fondos",
            fondo);
    }


    // ==========================================
    // MÉTODO GENÉRICO
    // ==========================================

    private async Task<SyncResponseDto>
        EnviarAsync<T>(
            string endpoint,
            T contenido)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return new SyncResponseDto
            {
                Exito = false,

                Mensaje =
                    "La URL de Integration no está configurada."
            };
        }

        try
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_baseUrl}{endpoint}")
                {
                    Content = JsonContent.Create(contenido)
                };

            // El JWT que entregó Integration en el login viaja en cada envío.
            if (!string.IsNullOrWhiteSpace(_sessionService.Token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        _sessionService.Token);
            }

            using var response =
                await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new SyncResponseDto
                {
                    Exito = false,

                    Mensaje =
                        $"Integration respondió con estado {response.StatusCode}."
                };
            }

            var resultado =
                await response.Content
                    .ReadFromJsonAsync<
                        SyncResponseDto>();

            return resultado ??
                new SyncResponseDto
                {
                    Exito = true,

                    Mensaje =
                        "Sincronización completada."
                };
        }
        catch (Exception ex)
        {
            return new SyncResponseDto
            {
                Exito = false,

                Mensaje =
                    $"No fue posible contactar Integration: {ex.Message}"
            };
        }
    }
}
