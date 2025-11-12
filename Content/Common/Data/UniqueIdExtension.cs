using Il2CppReloaded.Gameplay;
using PvZReCoreLib.Util;

namespace PvZReCoreLib.Content.Common.Data;

public class UniqueIdExtension : ClassExtension<ReloadedObject>
{
    #region Variables

    public Guid UniqueId;

    #endregion

    #region Constructors
    
    public UniqueIdExtension()
    {
        RandomizeUniqueId();
    }

    #endregion

    #region Methods
    
    public void RandomizeUniqueId()
    {
        UniqueId = Guid.NewGuid();
    }

    #endregion
}