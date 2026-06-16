namespace {{ core_project_name }}.Utils.Caching;

public class CacheSettings
{
    public int SlidingExpirationMinutes { get; set; } = 30;
    public int AbsoluteExpirationMinutes { get; set; } = 120;
}
