using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsStackExchangeAPI
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {

            CreateMap<StackExchangeResponseItem, StackExchangeResponseItemEntity>().ConstructUsing(source => new StackExchangeResponseItemEntity(source.creation_date, source.question_id));

        }
    }
}
