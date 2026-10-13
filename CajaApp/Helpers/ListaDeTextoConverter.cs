using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CajaApp.Helpers;

/// <summary>
/// Lee un campo JSON que puede venir como texto ("Cajero") o como lista
/// (["Cajero", "Administrador"]). Así el login funciona tanto con el
/// formato anterior de un solo rol como con la lista de roles del CORE.
/// </summary>
public class ListaDeTextoConverter : JsonConverter<List<string>>
{
    public override List<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var lista = new List<string>();

        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return lista;

            case JsonTokenType.String:
                AgregarSiTieneTexto(lista, reader.GetString());
                return lista;

            case JsonTokenType.StartArray:
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        AgregarSiTieneTexto(lista, reader.GetString());
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                return lista;

            default:
                reader.Skip();
                return lista;
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        List<string> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }

    private static void AgregarSiTieneTexto(List<string> lista, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
        {
            lista.Add(valor.Trim());
        }
    }
}
