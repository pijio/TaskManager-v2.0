using System.Collections.Concurrent;
using System.Diagnostics;

namespace TaskManager.Server;

public class LimitsHelper
{
    
    public Limits Limits { get; set; }

    private ConcurrentDictionary<LimitType, Func<JobInfo, Limits, bool>> LimitTypeToExpressionMap;
    LimitType[] Types = (LimitType[])Enum.GetValues(typeof(LimitType));
    public LimitsHelper(Limits limits)
    {
        Limits = limits;
        LimitTypeToExpressionMap = new ConcurrentDictionary<LimitType, Func<JobInfo, Limits, bool>>();
        FillMap(LimitTypeToExpressionMap);

    }

    public bool CheckLimit(JobInfo proc, LimitType type)
        => LimitTypeToExpressionMap[type](proc, Limits);

    public bool CheckAll(JobInfo proc, out List<LimitType> exceedBy)
    {
        exceedBy = new List<LimitType>();
        foreach (var lim in Types)
        {
            if (LimitTypeToExpressionMap[lim](proc, Limits))
            {
                exceedBy.Add(lim);
            }
        }
        return exceedBy.Count != 0;
    }
    private static void FillMap(ConcurrentDictionary<LimitType, Func<JobInfo, Limits, bool>> map)
    {
        map.TryAdd(LimitType.MemoryLimit, (proc, lims) => proc.Memory > lims.MemoryLimit);
        map.TryAdd(LimitType.AbsoluteTimelimit, (proc, lims) => proc.AbsoluteTime > lims.AbsoluteTimeLimit);
        map.TryAdd(LimitType.ProcessorTimeLimit, (proc, lims) => proc.ProcessorTime > lims.ProcessorTimeLimit);
    }
}