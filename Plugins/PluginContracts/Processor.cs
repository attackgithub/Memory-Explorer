namespace PluginContracts
{
    public interface IProcessor
    {
        string Name { get; }
        void Do();
    }
}
