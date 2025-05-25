namespace CascadePass.Glazier.UI.Tests
{
    public class MockRegistryProvider : IRegistryProvider
    {
        public object ReturnValue { get; set; }

        public object GetValue(string keyName, string valueName)
        {
            return this.ReturnValue;
        }
    }
}
