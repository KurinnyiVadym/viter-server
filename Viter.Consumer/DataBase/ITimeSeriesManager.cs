namespace Viter.Consumer.DataBase;

public interface ITimeSeriesManager
{
    ValueTask<string> GetKeyForDevice(string keyPrefix, string deviceId);

}