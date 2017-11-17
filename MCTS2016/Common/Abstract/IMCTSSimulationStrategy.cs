using System;
using Common.Abstract;
using System.Collections.Generic;

namespace Common.Abstract
{
    public interface IMCTSSimulationStrategy : ISimulationStrategy
    {
        int iterations { get; set; }
    }
}

