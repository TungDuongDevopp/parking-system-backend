

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
            .ForMember(d => d.DateOfBirth, opt =>
                opt.PreCondition(s => s.DateOfBirth.HasValue))
            .ForMember(d => d.Gender, opt =>
                opt.PreCondition(s => s.Gender.HasValue))
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) =>
                    srcMember != null && !(srcMember is string s && string.IsNullOrWhiteSpace(s))));
        CreateMap<ChangeStatusDto, Staff>();
      
        CreateMap<ChangeProfileDto, Staff>()
            .ForMember(d => d.Gender, opt =>
                opt.PreCondition(s => s.Gender.HasValue))
            .ForMember(d => d.DateOfBirth, opt =>
                opt.PreCondition(s => s.DateOfBirth.HasValue))
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) =>
                    srcMember != null && !(srcMember is string s && string.IsNullOrWhiteSpace(s))));
    }
}
