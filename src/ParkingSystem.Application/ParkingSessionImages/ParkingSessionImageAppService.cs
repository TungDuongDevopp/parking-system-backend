using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authorization;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.FileStorage;
using ParkingSystem.ParkingSessionImages.Dto;
using System.Threading.Tasks;
namespace ParkingSystem.ParkingSessionImages;


[AbpAuthorize(PermissionNames.Pages_ParkingSessions_Image)]
public class ParkingSessionImageAppService : ParkingSystemAppServiceBase, IParkingSessionImageAppService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IRepository<Staff,long> _staffRepository;
    private readonly IRepository<ParkingSessionImage, long> _repository;
    private readonly IRepository<ParkingSession, long> _parkingSessionRepository;
    public ParkingSessionImageAppService(IFileStorageService fileStorageService, IRepository<ParkingSessionImage, long> repository,
        IRepository<Staff, long> staffRepository,
       IRepository<ParkingSession, long> parkingSessionRepository)
    {
        _fileStorageService = fileStorageService;
        _repository = repository;
        _staffRepository = staffRepository;
        _parkingSessionRepository = parkingSessionRepository;
    }
    private async Task CheckImageViewAccessAsync(
     ParkingSessionImage image)
    {
        var canViewAll = await PermissionChecker.IsGrantedAsync(
            PermissionNames.Pages_ParkingSession_Image_ViewAll
        );

        if (canViewAll)
            return;

        var userId = AbpSession.UserId;

        if (!userId.HasValue)
        {
            throw new AbpAuthorizationException(
                "You must be logged in."
            );
        }

        var staff = await _staffRepository.FirstOrDefaultAsync(
            x => x.UserId == userId.Value
        );

        if (staff == null)
        {
            throw new AbpAuthorizationException(
                "You do not have permission to view this image."
            );
        }

        var session = await _parkingSessionRepository.GetAsync(
            image.ParkingSessionId
        );

        bool canView = image.Type switch
        {
            SessionImageType.checkIn =>
                session.CheckInStaffId == staff.Id || session.CheckOutStaffId == staff.Id,

            SessionImageType.checkOut =>
                session.CheckOutStaffId == staff.Id,

            _ => false
        };

        if (!canView)
        {
            throw new AbpAuthorizationException(
                "You do not have permission to view this image."
            );
        }
    }
    public Task<FileStreamResult> DownloadAsync(EntityDto<long> input)
    {
        throw new System.NotImplementedException();
    }

    public Task<PagedResultDto<ParkingSessionImageDto>> GetAllAsync(PagedImageResultRequestDto input)
    {
        throw new System.NotImplementedException();
    }

    public Task<ParkingSessionImageDto> GetAsync(EntityDto<long> input)
    {
        throw new System.NotImplementedException();
    }

    public Task<ParkingSessionImageDto> UploadAsync(UploadImageRequestDto image)
    {
        throw new System.NotImplementedException();
    }
}
