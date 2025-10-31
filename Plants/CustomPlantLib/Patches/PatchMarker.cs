using PvZReCoreLib.Util;

namespace PvZReCoreLib.Plants.CustomPlantLib.Patches;

public class PatchMarker<TClassType> : ClassExtension<TClassType> where TClassType : Il2CppSystem.Object
{
    public bool IsPatched;
}