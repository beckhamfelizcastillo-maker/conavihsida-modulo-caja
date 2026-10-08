using CajaApp.DTOs;
using CajaApp.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    private readonly string _baseUrl;

    private readonly bool _useMockAuthentication;

    public bool EsModoDemo => _useMockAuthentication;


    public AuthService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;

        _baseUrl =
            configuration["ApiSettings:BaseUrl"]
            ?? string.Empty;

        _useMockAuthentication =
            configuration.GetValue<bool>(
                "ApiSettings:UseMockAuthentication");
    }


    public async Task<LoginResponseDto> LoginAsync(
        string usuario,
        string password)
    {
        // ========================================
        // MODO DESARROLLO
        // ========================================

        if (_useMockAuthentication)
        {
            await Task.Delay(600);

            if (usuario == "cajero" &&
                password == "1234")
            {
                return new LoginResponseDto
                {
                    Exito = true,

                    Token =
                        "TOKEN-DEMO-CAJA",

                    Rol =
                        "Cajero",

                    NombreCompleto =
                        "María Rosario",

                    Mensaje =
                        "Autenticación correcta."
                };
            }

            return new LoginResponseDto
            {
                Exito = false,

                Mensaje =
                    "Usuario o contraseña incorrectos."
            };
        }


        // ========================================
        // API REAL
        // ========================================

        try
        {
            var request =
                new LoginRequestDto
                {
                    Usuario = usuario,

                    Password = password
                };

            var response =
                await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/auth/login",
                    request);

            if (response.IsSuccessStatusCode)
            {
                var resultado =
                    await response.Content
                        .ReadFromJsonAsync<LoginResponseDto>();

                return resultado ??
                    new LoginResponseDto
                    {
                        Exito = false,

                        Mensaje =
                            "Respuesta vacía del servidor."
                    };
            }

            return new LoginResponseDto
            {
                Exito = false,

                Mensaje =
                    $"Credenciales inválidas o error del servidor ({response.StatusCode})."
            };
        }
        catch (Exception ex)
        {
            return new LoginResponseDto
            {
                Exito = false,

                Mensaje =
                    $"No se pudo conectar con el servidor: {ex.Message}"
            };
        }
    }
}
