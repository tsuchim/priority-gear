namespace PriorityGear.App.Runtime;

public interface IForegroundProcessSource
{
    int? GetForegroundProcessId();
}
