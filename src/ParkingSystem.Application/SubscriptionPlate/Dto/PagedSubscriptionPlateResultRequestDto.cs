using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.SubscriptionPlate.Dto;
public class PagedSubscriptionPlateResultRequestDto:  PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }
    public long? SubscriptionId { get; set; }

}
