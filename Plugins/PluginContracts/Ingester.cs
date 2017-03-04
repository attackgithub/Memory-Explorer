namespace PluginContracts
{
    public interface IIngester
    {
        string Name { get; }
        void Do();
    }
}
