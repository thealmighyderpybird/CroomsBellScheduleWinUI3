﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CroomsBellScheduleC_.Provider
{
    public class CacheProvider(IBellScheduleProvider actualProvider) : IBellScheduleProvider
    {
        private readonly IBellScheduleProvider _bellScheduleProvider = actualProvider;
        private BellScheduleReader? _cache;
        private int CacheDay;

        public bool RequiresUpdate
        {
            get
            {
                return CacheDay != DateTime.Now.DayOfYear;
            }
        }

        public async Task<BellScheduleReader> GetTodayActivity()
        {
            if (_cache == null)
            {
                _cache = await _bellScheduleProvider.GetTodayActivity();
                CacheDay = DateTime.Now.DayOfYear;
                return _cache;
            }

            if (CacheDay != DateTime.Now.DayOfYear)
            {
                _cache = await _bellScheduleProvider.GetTodayActivity();
                CacheDay = DateTime.Now.DayOfYear;
                return _cache;
            }

            return _cache;
        }
    }
}
