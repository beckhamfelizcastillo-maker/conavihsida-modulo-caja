using CajaApp.DTOs;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface IApiService
{
    Task<SubsidioResponseDto> ValidarSubsidioAsync(string cedula);
}