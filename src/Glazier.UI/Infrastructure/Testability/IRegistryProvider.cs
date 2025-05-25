namespace CascadePass.Glazier.UI
{
    public interface IRegistryProvider
    {
        object GetValue(string keyName, string valueName);
    }
}