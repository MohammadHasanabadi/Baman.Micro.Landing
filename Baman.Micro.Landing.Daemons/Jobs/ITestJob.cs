using Baman.Micro.Landing.Data;
using Manex.CoreLib.Scheduling;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Baman.Micro.Landing.Daemons.Jobs
{
    [Worker, DisallowConcurrentExecution]
    internal class ITestJob : IJob
    {
        private readonly EntityContext _context;

        public ITestJob(EntityContext context)
        {
            _context = context;
        }

        public Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
