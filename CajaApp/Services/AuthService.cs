using CajaApp.DTOs;
using CajaApp.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using CajaApp.Helpers;
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

                    Roles =
                        new() { "Cajero" },

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

                // Si el Gateway responde 200 con token pero sin el campo
                // "exito", se considera un login correcto.
                if (resultado is not null &&
                    !resultado.Exito &&
                    !string.IsNullOrWhiteSpace(resultado.Token))
                {
                    resultado.Exito = true;
                }

                // Guarda el acceso para poder entrar si luego se cae el servidor.
                if (resultado is not null &&
                    resultado.Exito &&
                    RolesHelper.ResolverRolCaja(resultado.Rol, resultado.Roles) is not null)
                {
                    var roles = new System.Collections.Generic.List<string>(resultado.Rol);
                    roles.AddRange(resultado.Roles);

                    SesionOfflineStore.Guardar(
                        usuario,
                        password,
                        resultado.NombreCompleto,
                        roles,
                        resultado.Token);
                }

                return resultado ??
                    new LoginResponseDto
                    {
                        Exito = false,

                        Mensaje =
                            "Respuesta vacía del servidor."
                    };
            }

            // 5xx: Integración responde pero el CORE está caído → sin conexión.
            if ((int)response.StatusCode >= 500)
            {
                return LoginSinConexion(usuario, password, $"el servidor respondió {(int)response.StatusCode}");
            }

            return new LoginResponseDto
            {
                Exito = false,

                Mensaje =
                    $"Credenciales inválidas o error del servidor ({response.StatusCode})."
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return LoginSinConexion(usuario, password, ex.Message);
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


    // ========================================
    // OPERACIÓN CONTINUA: LOGIN SIN CONEXIÓN
    // ========================================

    private static LoginResponseDto LoginSinConexion(
        string usuario,
        string password,
        string causa)
    {
        var guardada =
            SesionOfflineStore.Validar(usuario, password);

        if (guardada is null)
        {
            return new LoginResponseDto
            {
                Exito = false,

                Mensaje =
                    $"No se pudo conectar con el servidor ({causa}). " +
                    "Sin conexión solo pueden entrar usuarios que ya iniciaron sesión en esta terminal en los últimos " +
                    $"{SesionOfflineStore.DiasValidez} días."
            };
        }

        return new LoginResponseDto
        {
            Exito = true,
            SinConexion = true,
            Token = guardada.Token,
            Roles = guardada.Roles,
            NombreCompleto = guardada.NombreCompleto,
            Mensaje = "Sesión iniciada sin conexión. Las operaciones se sincronizarán al volver el servidor."
        };
    }
}
