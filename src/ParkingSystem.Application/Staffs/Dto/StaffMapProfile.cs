

using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.Staffs.Dto;

public class StaffMapProfile : Profile
{
   public StaffMapProfile()
    {
        CreateMap<Staff,StaffDto>();
        CreateMap<CreateStaffDto, Staff>();
        CreateMap<UpdateStaffDto, Staff>()
            .ForMember(d => d.HiredDate, opt =>
                opt.PreCondition(s => s.HiredDate.HasValue))
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<ChangeStatusDto, Staff>();
      
        CreateMap<ChangeProfileDto, Staff>()      
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember != null)) ;
    }
}
