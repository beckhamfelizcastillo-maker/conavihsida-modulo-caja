using CajaApp.Helpers;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CajaApp.DTOs;

public class LoginResponseDto
{
    public bool Exito { get; set; }
    public string Token { get; set; } = string.Empty;

    /// <summary>Formato anterior: un solo rol. Acepta también una lista.</summary>
    [JsonConverter(typeof(ListaDeTextoConverter))]
    public List<string> Rol { get; set; } = new();

    /// <summary>Formato del CORE: lista de roles. Caja elige uno con RolesHelper.</summary>
    [JsonConverter(typeof(ListaDeTextoConverter))]
    public List<string> Roles { get; set; } = new();

    public string NombreCompleto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>true cuando el acceso se validó en la terminal porque el servidor no respondió.</summary>
    [JsonIgnore]
    public bool SinConexion { get; set; }
}