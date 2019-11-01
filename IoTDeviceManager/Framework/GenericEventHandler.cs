namespace IoTDeviceManager.Framework
{
    public delegate void GenericEventHandler<in T>(T sender);
    public delegate void GenericEventHandler<in T, in TArgs>(T sender, TArgs args);
}
