
using Abp.Application.Services.Dto;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.ParkingSessionImages.Dto;
using System.Threading.Tasks;

namespace ParkingSystem.ParkingSessionImages;

public interface IParkingSessionImageAppService
{

    Task<ParkingSessionImageDto> GetAsync(EntityDto<long> input);
    Task<PagedResultDto<ParkingSessionImageDto>> GetAllAsync(PagedImageResultRequestDto input);
    Task<FileStreamResult> DownloadAsync(EntityDto<long> input);
}
