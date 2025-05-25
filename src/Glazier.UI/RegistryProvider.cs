using Microsoft.Win32;

namespace CascadePass.Glazier.UI
{
    public class RegistryProvider : IRegistryProvider
    {
        public object GetValue(string keyName, string valueName)
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName);

            key?.Flush();
            return key?.GetValue(valueName);
        }
    }
}