

using Abp.Application.Services;
using ParkingSystem.ParkingAreas.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParkingSystem.ParkingAreas;

public interface IParkingAreaAppService: IAsyncCrudAppService<ParkingAreaDto,long,PagedParkingAreaResultRequestDto,CreateParkingAreaDto,UpdateParkingAreaDto>
{
    Task<ParkingAreaDto> ChangeStatus(ChangeStatusDto input);
    Task<List<ParkingAreaLookupDto>> GetAvailableAreasAsync();
}

