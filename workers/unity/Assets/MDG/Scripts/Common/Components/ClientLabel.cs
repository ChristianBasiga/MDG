using Unity.Entities;

// To signify collided with entity that is not ally
namespace MDG.Common
{
    // Literally just to signify. Maybe down line could have another component 
    // and store other info, but really I just need it for query at this point.
    public struct Enemy: IComponentData { }

}